// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Contracts.Federation;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Represents an implementation of <see cref="IFederationServeService"/>.
/// </summary>
/// <remarks>
/// Events are appended to the inbox through the same <see cref="IFederationInbox"/> a pull consumer uses, so delivery is at least once,
/// duplicates are dropped per origin, and the cursor of the session is the tail of that inbox: a session on any silo, after any
/// connection loss, resumes from the last event that really arrived. Only the target of the policy entry is ever addressed, and an event
/// whose origin is this store is rejected so two stores that serve each other cannot loop.
/// </remarks>
/// <param name="caller">The <see cref="IFederationCaller"/> giving the presented client certificate.</param>
/// <param name="peerAdmission">The <see cref="IFederationPeerAdmission"/> admitting the peer by chain, registry and policy.</param>
/// <param name="inbox">The <see cref="IFederationInbox"/> the events are appended to.</param>
/// <param name="subscriptions">The <see cref="IFederationSubscriptions"/> deciding which events are wanted.</param>
/// <param name="status">The <see cref="IFederationStatus"/> to record on.</param>
/// <param name="rateLimiter">The <see cref="FederationRateLimiter"/>.</param>
/// <param name="metrics">The <see cref="FederationMetrics"/>.</param>
/// <param name="consumerMetrics">The <see cref="IFederationConsumerMetrics"/>.</param>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> for the session grain.</param>
/// <param name="options">The <see cref="IOptions{TOptions}"/> of <see cref="ChronicleOptions"/>.</param>
/// <param name="logger">The <see cref="ILogger{TCategoryName}"/>.</param>
/// <param name="timing">The optional <see cref="FederationConsumerTiming"/>.</param>
/// <param name="timeProvider">The optional <see cref="TimeProvider"/>.</param>
[Singleton]
public class FederationServeService(
    IFederationCaller caller,
    IFederationPeerAdmission peerAdmission,
    IFederationInbox inbox,
    IFederationSubscriptions subscriptions,
    IFederationStatus status,
    FederationRateLimiter rateLimiter,
    FederationMetrics metrics,
    IFederationConsumerMetrics consumerMetrics,
    IGrainFactory grainFactory,
    IOptions<ChronicleOptions> options,
    ILogger<FederationServeService> logger,
    FederationConsumerTiming? timing = null,
    TimeProvider? timeProvider = null) : IFederationServeService
{
    readonly FederationConsumerTiming _timing = timing ?? FederationConsumerTiming.Default;
    readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    /// <summary>
    /// Gets the logical source of the inbox the events of a subject are appended to, and the id of its link in status and metrics.
    /// </summary>
    /// <param name="subject">The subject.</param>
    /// <returns>The logical source. Event store subscriptions of the target store name it as their source.</returns>
    public static string LogicalSourceOf(string subject) => $"hosted-{subject}";

    /// <inheritdoc/>
    public async IAsyncEnumerable<ServePullRequest> Serve(IAsyncEnumerable<ServeBatch> batches, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var target = await Admit();
        if (target is null)
        {
            metrics.Denied(FederationMetrics.UnknownLink);
            yield return DeniedFrame();
            yield break;
        }

        var subject = target.Subject;
        var linkId = LogicalSourceOf(subject);
        var connectionId = Guid.NewGuid().ToString("N");
        var session = grainFactory.GetGrain<IFederationSession>(subject);
        await session.Open(connectionId);

        var endReason = "the dialer ended the call";
        try
        {
            await using var answers = batches.GetAsyncEnumerator(cancellationToken);
            var state = new SessionState();
            var backoff = TimeSpan.Zero;

            while (!cancellationToken.IsCancellationRequested)
            {
                target = await Admit();
                if (target is null)
                {
                    endReason = "the direction was refused";
                    yield return DeniedFrame();
                    yield break;
                }

                if (!await session.Renew(connectionId))
                {
                    endReason = "a newer connection superseded it";
                    yield break;
                }

                var binding = new FederationBindingState(
                    linkId,
                    linkId,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    target.Entry.EventStore,
                    target.Entry.Namespace,
                    FederationOrigin.Runtime);

                var selector = new FederationEventSelector(await subscriptions.Get(binding));
                var wanted = selector.EventTypeIds.Where(_ => Allows(target.Entry, _)).ToArray();
                if (wanted.Length == 0)
                {
                    SetStatus(linkId, FederationBindingStates.Idle, state, null);
                    await Task.Delay(_timing.PollInterval, _timeProvider, cancellationToken);
                    continue;
                }

                if (!state.Resumed || state.Target != (target.Entry.EventStore, target.Entry.Namespace))
                {
                    await Resume(binding, state);
                    logger.ServeStarted(subject, state.Cursor);
                }

                var limits = options.Value.Federation.Limits;
                if (!rateLimiter.TryBegin(linkId, limits.MaxEventsPerSecond, limits.MaxBytesPerSecond))
                {
                    metrics.Throttled(linkId);
                    backoff = _timing.NextBackoff(backoff);
                    SetStatus(linkId, FederationBindingStates.Backoff, state, "The inbound direction is rate limited");
                    await Task.Delay(backoff, _timeProvider, cancellationToken);
                    continue;
                }

                state.RequestId++;
                yield return new ServePullRequest
                {
                    RequestId = state.RequestId,
                    FromSequenceNumber = state.Cursor,
                    Limit = limits.MaxBatchSize,
                    EventTypes = wanted
                };

                var answer = await Next(answers, cancellationToken);
                if (answer is null || answer.RequestId != state.RequestId)
                {
                    endReason = answer is null ? "the dialer did not answer in time or closed the call" : "the dialer answered another request";
                    yield break;
                }

                var outcome = answer.Batch.Status switch
                {
                    FederatedEventBatchStatus.Ok => await Forward(subject, linkId, binding, selector, target.Entry, answer.Batch, state),
                    FederatedEventBatchStatus.Throttled => Outcome.Slow,
                    _ => Outcome.Failed("the dialer answered with an unknown status")
                };

                if (outcome.Failure is not null)
                {
                    endReason = outcome.Failure;
                    consumerMetrics.Error(linkId, "append");
                    SetStatus(linkId, FederationBindingStates.Backoff, state, outcome.Failure);
                    yield break;
                }

                if (outcome.Slowed)
                {
                    backoff = _timing.NextBackoff(backoff);
                    SetStatus(linkId, FederationBindingStates.Backoff, state, "The dialer throttled the inbound direction");
                    await Task.Delay(backoff, _timeProvider, cancellationToken);
                    continue;
                }

                backoff = TimeSpan.Zero;
                SetStatus(linkId, FederationBindingStates.Streaming, state, null);
                if (outcome.Count == 0)
                {
                    await Task.Delay(_timing.PollInterval, _timeProvider, cancellationToken);
                }
            }
        }
        finally
        {
            logger.ServeEnded(subject, endReason);
            await session.Close(connectionId);
        }
    }

    static ServePullRequest DeniedFrame() => new() { Status = FederatedEventBatchStatus.Denied };

    static bool Allows(FederationPolicyEntry entry, string eventType) =>
        entry.AllPublic || entry.EventTypes.Contains(eventType.Split('+')[0], StringComparer.Ordinal);

    static ForwardedEventGuard NewGuard(FederationBindingState binding, FederationResumePoint? last)
    {
        var guard = new ForwardedEventGuard(new EventStoreName(binding.TargetEventStore));
        if (last is not null)
        {
            guard.Evaluate(new EventStoreName(last.OriginEventStore), last.LastSourceSequenceNumber);
        }

        return guard;
    }

    async Task<InboundTarget?> Admit()
    {
        var admission = await peerAdmission.Admit(caller.ClientCertificate);
        if (!admission.IsAdmitted)
        {
            logger.ServeRefused(admission.Refusal.ToString());
            return null;
        }

        var entry = admission.Policy!.EntryFor(FederationDirection.AcceptorInbound);
        if (entry is null)
        {
            logger.ServeRefused(nameof(FederationAdmissionRefusal.NoPolicy));
            return null;
        }

        return new(admission.Subject, entry);
    }

    async Task<ServeBatch?> Next(IAsyncEnumerator<ServeBatch> answers, CancellationToken cancellationToken)
    {
        try
        {
            return await answers.MoveNextAsync().AsTask().WaitAsync(_timing.PullTimeout, _timeProvider, cancellationToken)
                ? answers.Current
                : null;
        }
        catch (TimeoutException)
        {
            return null;
        }
    }

    async Task Resume(FederationBindingState binding, SessionState state)
    {
        var resumePoint = await inbox.GetResumePoint(binding);
        state.Last = resumePoint;
        state.Cursor = resumePoint is null ? 0 : resumePoint.LastSourceSequenceNumber + 1;
        state.Guard = NewGuard(binding, resumePoint);
        state.Target = (binding.TargetEventStore, binding.TargetNamespace);
        state.Resumed = true;
    }

    async Task<Outcome> Forward(
        string subject,
        string linkId,
        FederationBindingState binding,
        FederationEventSelector selector,
        FederationPolicyEntry entry,
        FederatedEventBatchResponse batch,
        SessionState state)
    {
        var events = batch.Events.OrderBy(_ => _.SequenceNumber).ToArray();
        consumerMetrics.EventsPulled(linkId, events.Length);
        state.Tail = batch.TailSequenceNumber;

        var origin = batch.OriginEventStore;
        if (events.Length > 0 && string.IsNullOrEmpty(origin))
        {
            return Outcome.Failed("the dialer named no origin event store");
        }

        var guard = state.Guard ??= NewGuard(binding, state.Last);
        var appended = 0;
        var bytes = 0L;
        foreach (var contractEvent in events)
        {
            var decision = guard.Evaluate(new EventStoreName(origin), contractEvent.SequenceNumber);
            if (decision != ForwardedEventDecision.Accept)
            {
                if (decision == ForwardedEventDecision.RejectOwnOrigin)
                {
                    logger.ServeOwnOriginRejected(subject);
                    consumerMetrics.EventSkipped(linkId, FederationSkipReasons.OwnOrigin);
                }
                else
                {
                    consumerMetrics.EventSkipped(linkId, FederationSkipReasons.Duplicate);
                }

                continue;
            }

            if (!selector.Wants(contractEvent.EventType) || !Allows(entry, contractEvent.EventType))
            {
                consumerMetrics.EventSkipped(linkId, FederationSkipReasons.Filtered);
                continue;
            }

            var failure = await Append(binding, origin, contractEvent);
            if (failure is not null)
            {
                // The guard has seen this event already; rebuild it from what is really in the inbox so the retry is not mistaken for a duplicate.
                state.Guard = NewGuard(binding, await inbox.GetResumePoint(binding));
                consumerMetrics.EventsAppended(linkId, appended);
                logger.ServeAppendFailed(subject, failure);
                return Outcome.Failed(failure);
            }

            appended++;
            bytes += Encoding.UTF8.GetByteCount(contractEvent.Content);
            state.Last = new FederationResumePoint(origin, contractEvent.SequenceNumber);
        }

        consumerMetrics.EventsAppended(linkId, appended);
        rateLimiter.Consume(linkId, events.Length, bytes);
        metrics.Delivered(linkId, appended, bytes);

        var next = Math.Max(batch.NextSequenceNumber, events.Length > 0 ? events[^1].SequenceNumber + 1 : 0);
        state.Cursor = Math.Max(state.Cursor, next);
        state.LastDelivery = events.Length > 0 ? _timeProvider.GetUtcNow() : state.LastDelivery;
        return Outcome.Delivered(events.Length);
    }

    async Task<string?> Append(FederationBindingState binding, string origin, Contracts.Federation.FederatedEvent contractEvent)
    {
        try
        {
            return await inbox.Append(binding, origin, contractEvent.ToCore());
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return FederationErrorScrubber.Scrub(exception);
        }
    }

    void SetStatus(string linkId, string stateName, SessionState state, string? error) =>
        status.SetBinding(linkId, new FederationBindingStatus(stateName, state.Last?.LastSourceSequenceNumber ?? 0, state.Tail, state.LastDelivery, error));

    sealed record InboundTarget(string Subject, FederationPolicyEntry Entry);

    sealed class SessionState
    {
        public bool Resumed { get; set; }

        public (string EventStore, string Namespace) Target { get; set; }

        public ulong Cursor { get; set; }

        public ulong RequestId { get; set; }

        public ulong Tail { get; set; }

        public DateTimeOffset? LastDelivery { get; set; }

        public FederationResumePoint? Last { get; set; }

        public ForwardedEventGuard? Guard { get; set; }
    }

    sealed record Outcome(string? Failure, bool Slowed, int Count)
    {
        public static Outcome Slow { get; } = new(null, true, 0);

        public static Outcome Failed(string failure) => new(failure, false, 0);

        public static Outcome Delivered(int count) => new(null, false, count);
    }
}
