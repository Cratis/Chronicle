// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProtoBuf.Grpc;
using ContractFederatedEvent = Cratis.Chronicle.Contracts.Federation.FederatedEvent;
using ContractPullRequest = Cratis.Chronicle.Contracts.Federation.PullRequest;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// The logic of one federation link: pull a batch from the producer, forward it into the local inbox, and say how long to wait before the next step.
/// One instance serves one binding and holds no state shared with other bindings, so a slow or failing link never affects another.
/// </summary>
/// <param name="bindingId">The binding id.</param>
/// <param name="registry">The federation registry.</param>
/// <param name="status">The federation status.</param>
/// <param name="clients">The factory of producer connections.</param>
/// <param name="inbox">The local inbox.</param>
/// <param name="subscriptions">The local subscriptions.</param>
/// <param name="metrics">The metrics.</param>
/// <param name="options">The Chronicle options.</param>
/// <param name="logger">The logger.</param>
/// <param name="timing">The timing policy.</param>
/// <param name="timeProvider">The time provider.</param>
public sealed class FederationLinkPump(
    string bindingId,
    IFederationRegistry registry,
    IFederationStatus status,
    IFederationPullClientFactory clients,
    IFederationInbox inbox,
    IFederationSubscriptions subscriptions,
    IFederationConsumerMetrics metrics,
    IOptions<ChronicleOptions> options,
    ILogger<FederationLinkPump> logger,
    FederationConsumerTiming? timing = null,
    TimeProvider? timeProvider = null) : IDisposable
{
    readonly FederationConsumerTiming _timing = timing ?? FederationConsumerTiming.Default;
    readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    FederationBindingState? _binding;
    IFederationPullConnection? _connection;
    ForwardedEventGuard? _guard;
    bool _resumed;
    ulong _cursor;
    FederationResumePoint? _lastForwarded;
    ulong _tail;
    DateTimeOffset? _lastDelivery;
    TimeSpan _backoff = TimeSpan.Zero;

    /// <inheritdoc/>
    public void Dispose() => CloseConnection();

    /// <summary>
    /// Do one step of the link.
    /// </summary>
    /// <param name="cancellationToken">Cancellation, when the owner is going away.</param>
    /// <returns>The time to wait before the next step, or null when the link is finished (removed or disabled).</returns>
    public async Task<TimeSpan?> Step(CancellationToken cancellationToken)
    {
        try
        {
            return await StepCore(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            // The connection may be the problem; a fresh one is made after the backoff.
            CloseConnection();
            return Fail(FederationErrorScrubber.Scrub(exception, Secrets()), "pull");
        }
    }

    static ContractPullRequest CreateRequest(FederationBindingState binding, ulong cursor, int limit, IEnumerable<string> eventTypes) => new()
    {
        EventStore = binding.RemoteEventStore,
        Namespace = binding.RemoteNamespace,
        FromSequenceNumber = cursor,
        Limit = limit,
        EventTypes = [.. eventTypes]
    };

    static FederatedEvent ToEvent(ContractFederatedEvent @event) => new(
        @event.SequenceNumber,
        @event.EventType,
        @event.Generation,
        @event.EventSourceType,
        @event.EventSourceId,
        @event.EventStreamType,
        @event.EventStreamId,
        @event.Occurred,
        @event.CorrelationId,
        @event.Subject,
        @event.Content,
        [.. @event.Tags]);

    async Task<TimeSpan?> StepCore(CancellationToken cancellationToken)
    {
        var federation = options.Value.Federation;
        var binding = federation.Enabled
            ? (await registry.GetBindings()).FirstOrDefault(_ => _.Id == bindingId)
            : null;

        if (binding is null)
        {
            CloseConnection();
            logger.LinkDisabled(bindingId);
            SetStatus(FederationBindingStates.Disabled, null);
            return null;
        }

        if (_binding is not null && _binding != binding)
        {
            CloseConnection();
            if (_binding.TargetEventStore != binding.TargetEventStore ||
                _binding.TargetNamespace != binding.TargetNamespace ||
                _binding.LogicalSource != binding.LogicalSource)
            {
                _resumed = false;
            }
        }

        _binding = binding;

        var selector = new FederationEventSelector(await subscriptions.Get(binding));
        if (selector.EventTypeIds.Count == 0)
        {
            // Nothing subscribes: pull nothing rather than everything.
            SetStatus(FederationBindingStates.Idle, null);
            return _timing.PollInterval;
        }

        if (!_resumed)
        {
            await Resume(binding);
        }

        if (_connection is null)
        {
            SetStatus(FederationBindingStates.Connecting, null);
            _connection = clients.Create(binding);
        }

        var request = CreateRequest(binding, _cursor, federation.Limits.MaxBatchSize, selector.EventTypeIds);
        var callOptions = new CallOptions(deadline: _timeProvider.GetUtcNow().UtcDateTime + _timing.PullTimeout, cancellationToken: cancellationToken);
        var result = await _connection.Service
            .Pull(request, new CallContext(callOptions))
            .WaitAsync(_timing.PullTimeout, _timeProvider, cancellationToken);

        if (!result.IsSuccess)
        {
            return Fail("The producer did not complete the pull", "rejected");
        }

        var batch = result.Data;
        switch (batch.Status)
        {
            case FederatedEventBatchStatus.Denied:
                // A revoked link stops here; it is retried only rarely, and never affects another binding.
                CloseConnection();
                metrics.Error(bindingId, "denied");
                logger.LinkDenied(bindingId);
                SetStatus(FederationBindingStates.Denied, "The producer denied the link");
                return _timing.DeniedBackoff;

            case FederatedEventBatchStatus.Throttled:
                metrics.Error(bindingId, "throttled");
                _backoff = _timing.NextBackoff(_backoff);
                SetStatus(FederationBindingStates.Backoff, "The producer throttled the link");
                return _backoff;

            case FederatedEventBatchStatus.Ok:
                return await Forward(binding, selector, batch);

            default:
                return Fail("The producer answered with an unknown status", "protocol");
        }
    }

    async Task<TimeSpan?> Forward(FederationBindingState binding, FederationEventSelector selector, Contracts.Federation.FederatedEventBatchResponse batch)
    {
        var events = batch.Events.OrderBy(_ => _.SequenceNumber).ToArray();
        _tail = batch.TailSequenceNumber;
        metrics.EventsPulled(bindingId, events.Length);

        var origin = batch.OriginEventStore;
        var guard = _guard ??= new ForwardedEventGuard(new EventStoreName(binding.TargetEventStore));
        var appended = 0;
        foreach (var contractEvent in events)
        {
            var decision = guard.Evaluate(new EventStoreName(origin), contractEvent.SequenceNumber);
            if (decision != ForwardedEventDecision.Accept)
            {
                if (decision == ForwardedEventDecision.RejectOwnOrigin)
                {
                    logger.OwnOriginRejected(bindingId);
                    metrics.EventSkipped(bindingId, FederationSkipReasons.OwnOrigin);
                }
                else
                {
                    metrics.EventSkipped(bindingId, FederationSkipReasons.Duplicate);
                }

                continue;
            }

            if (!selector.Wants(contractEvent.EventType))
            {
                metrics.EventSkipped(bindingId, FederationSkipReasons.Filtered);
                continue;
            }

            var failure = await Append(binding, origin, contractEvent);
            if (failure is not null)
            {
                // The guard has already seen this event; rebuild it from what is really in the inbox so the retry is not mistaken for a duplicate.
                RebuildGuard(binding);
                metrics.EventsAppended(bindingId, appended);
                return Fail(failure, "append");
            }

            appended++;
            _lastForwarded = new FederationResumePoint(origin, contractEvent.SequenceNumber);
        }

        metrics.EventsAppended(bindingId, appended);

        // The cursor moves past everything pulled, including skipped events, so the same run is not requested again while this grain is active.
        // It is only durable through appended events: after a restart the skipped run is simply pulled again and skipped again.
        var next = Math.Max(batch.NextSequenceNumber, events.Length > 0 ? events[^1].SequenceNumber + 1 : 0);
        _cursor = Math.Max(_cursor, next);

        _backoff = TimeSpan.Zero;
        _lastDelivery = events.Length > 0 ? _timeProvider.GetUtcNow() : _lastDelivery;
        SetStatus(FederationBindingStates.Streaming, null);

        // A non-empty batch means there may be more: go again immediately. Otherwise the link has caught up.
        return events.Length > 0 ? TimeSpan.Zero : _timing.PollInterval;
    }

    async Task<string?> Append(FederationBindingState binding, string origin, ContractFederatedEvent contractEvent)
    {
        try
        {
            return await inbox.Append(binding, origin, ToEvent(contractEvent));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return FederationErrorScrubber.Scrub(exception, Secrets());
        }
    }

    async Task Resume(FederationBindingState binding)
    {
        var resumePoint = await inbox.GetResumePoint(binding);
        _lastForwarded = resumePoint;
        _cursor = resumePoint is null ? 0 : resumePoint.LastSourceSequenceNumber + 1;
        RebuildGuard(binding);
        _resumed = true;
        logger.Resumed(bindingId, _cursor);
    }

    void RebuildGuard(FederationBindingState binding)
    {
        _guard = new ForwardedEventGuard(new EventStoreName(binding.TargetEventStore));
        if (_lastForwarded is not null)
        {
            _guard.Evaluate(new EventStoreName(_lastForwarded.OriginEventStore), _lastForwarded.LastSourceSequenceNumber);
        }
    }

    TimeSpan Fail(string scrubbed, string kind)
    {
        _backoff = _timing.NextBackoff(_backoff);
        metrics.Error(bindingId, kind);
        logger.LinkFailed(bindingId, kind, scrubbed);
        SetStatus(FederationBindingStates.Backoff, scrubbed);
        return _backoff;
    }

    void SetStatus(string state, string? error)
    {
        var lastSource = _cursor == 0 ? 0 : _cursor - 1;
        var current = new FederationBindingStatus(state, lastSource, _tail, _lastDelivery, error);
        status.SetBinding(bindingId, current);
        metrics.Lag(bindingId, current.Lag);
    }

    IEnumerable<string?> Secrets() => options.Value.Federation.Certificates.Values
        .SelectMany(_ => new[] { _.Path, _.PasswordPath })
        .Append(options.Value.Federation.CertificatePath)
        .Append(options.Value.Federation.CertificatePasswordPath);

    void CloseConnection()
    {
        _connection?.Dispose();
        _connection = null;
    }
}
