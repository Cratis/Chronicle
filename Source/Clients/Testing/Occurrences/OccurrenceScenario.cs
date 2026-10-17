// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Occurrences;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Storage.Occurrences;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Occurrences;
using Cratis.Chronicle.Testing.Events;
using Cratis.Chronicle.Transactions;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Testing.Occurrences;

/// <summary>
/// Drives one handler through real ingress, fenced claims, receipts, retry and completion in process.
/// There are no background workers, automatic lease renewals, reminders or automatic deliveries.
/// </summary>
/// <typeparam name="THandler">A concrete handler implementing exactly one IOccurrenceHandler interface.</typeparam>
public sealed partial class OccurrenceScenario<THandler> : IDisposable
    where THandler : class
{
    readonly Defaults _defaults;
    readonly Type _triggerType;
    readonly TriggerId _trigger;
    readonly Dictionary<OccurrenceId, OccurrenceSnapshotResponse?> _snapshots = [];
    readonly List<AppendedEvent> _events = [];
    ServiceProvider? _provider;
    EventStoreForTesting? _store;

    /// <summary>
    /// Initializes an isolated scenario using discovered artifacts.
    /// </summary>
    public OccurrenceScenario() : this(Defaults.Instance)
    {
    }

    /// <summary>
    /// Initializes an isolated scenario with per-run artifacts and optional explicit time.
    /// </summary>
    /// <param name="defaults">The artifacts and client settings.</param>
    public OccurrenceScenario(Defaults defaults)
    {
        _defaults = defaults;
        var targets = typeof(THandler).GetInterfaces().Where(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IOccurrenceHandler<>)).ToArray();
        if (targets.Length != 1) throw new TriggerNotValid("An occurrence scenario handler must implement exactly one IOccurrenceHandler<TTrigger>.");
        _triggerType = targets[0].GenericTypeArguments[0];
        _trigger = _triggerType.GetCustomAttribute<TriggerAttribute>()?.Id ?? throw new TriggerNotValid("The scenario handler needs an attributed trigger.");
        Time = defaults.ScenarioTime ?? new ScenarioTime();
        Services = new ServiceCollection();
        Services.AddLogging();
        Services.AddSingleton<IEventStore>(_ => EventStore);
        Services.AddSingleton<IEventLog>(_ => EventStore.EventLog);
        Services.AddSingleton<IUnitOfWorkManager>(_ => EventStore.UnitOfWorkManager);
    }

    /// <summary>
    /// Gets the constructor dependencies registered before the first operation.
    /// </summary>
    public IServiceCollection Services { get; }

    /// <summary>
    /// Gets the scenario's real client event store.
    /// </summary>
    public EventStoreForTesting EventStore => _store ??= new EventStoreForTesting(Provider, Catalog(), _defaults.ScenarioOptions, Time, _defaults.EventStore.Name, _defaults.EventStore.Namespace);

    /// <summary>
    /// Gets the explicitly driven clock.
    /// </summary>
    public ScenarioTime Time { get; }

    /// <summary>
    /// Gets the commit-then-drop-reply driver.
    /// </summary>
    public DropReplyInterceptor Replies => EventStore.Replies;

    ServiceProvider Provider => _provider ??= Services.BuildServiceProvider();
    Contracts.Occurrences.IOccurrences Wire => ((IChronicleServicesAccessor)EventStore.Connection).Services.Occurrences;

    /// <summary>
    /// Records an application trigger, collecting its durable state without invoking the handler.
    /// </summary>
    /// <typeparam name="TTrigger">The trigger type.</typeparam>
    /// <param name="trigger">The plaintext trigger payload.</param>
    /// <param name="idempotencyId">The stable retry identity.</param>
    /// <returns>The durable ingress result.</returns>
    public async Task<OccurrenceRecordResult> Record<TTrigger>(TTrigger trigger, string idempotencyId)
        where TTrigger : notnull
    {
        var result = await EventStore.Occurrences.Record(trigger, idempotencyId);
        await StatusOf(result.Id);
        return result;
    }

    /// <summary>
    /// Delivers the next claimable occurrence, then completes or records the handler's failure.
    /// </summary>
    /// <returns>The delivery outcome.</returns>
    public Task<OccurrenceDeliveryOutcome> DeliverNext() => Deliver(true, null);

    /// <summary>
    /// Claims the next occurrence and substitutes an injected handler failure, recording normal backoff or quarantine.
    /// </summary>
    /// <param name="exception">The injected failure; the handler is not invoked.</param>
    /// <returns>The delivery outcome.</returns>
    public Task<OccurrenceDeliveryOutcome> DeliverNextFailing(Exception exception) => Deliver(true, exception);

    /// <summary>
    /// Invokes the next handler but leaves a successful lease uncompleted, simulating loss before acknowledgement.
    /// </summary>
    /// <returns>Delivered on handler success; other outcomes have their usual meaning.</returns>
    public Task<OccurrenceDeliveryOutcome> DeliverWithoutCompleting() => Deliver(false, null);

    /// <summary>
    /// Reads and collects a fresh durable snapshot. Assertions use collected state and never perform asynchronous work.
    /// </summary>
    /// <param name="id">The occurrence identity.</param>
    /// <returns>The current snapshot, or null for an unknown identity.</returns>
    public async Task<OccurrenceSnapshotResponse?> StatusOf(OccurrenceId id)
    {
        var snapshot = await Wire.GetOccurrence(new() { EventStore = EventStore.Name, Namespace = EventStore.Namespace, OccurrenceId = id }).EnsureSuccess();
        if (snapshot is not null && string.IsNullOrEmpty(snapshot.Id)) snapshot = null;
        _snapshots[id] = snapshot;
        return snapshot;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _store?.Dispose();
        _provider?.Dispose();
    }

    async Task<OccurrenceDeliveryOutcome> Deliver(bool complete, Exception? injected)
    {
        // Force local trigger discovery/registration without attaching production background workers.
        _ = EventStore.Occurrences;
        var claims = await Wire.ClaimOccurrences(new() { EventStore = EventStore.Name, Namespace = EventStore.Namespace, Trigger = _trigger, WorkerId = "occurrence-scenario", LeaseSeconds = 60, Max = 1 }).EnsureSuccess();
        var claim = claims.Claims.SingleOrDefault();
        if (claim is null) return OccurrenceDeliveryOutcome.NothingDue;
        var context = new OccurrenceContext(claim.Id, claim.Trigger, (Chronicle.Occurrences.OccurrenceKind)claim.Kind, claim.Due is null ? null : (DateTimeOffset)claim.Due, claim.RecordedAt, null, claim.Attempts, new(claim.Id, claim.Epoch)) { Namespace = EventStore.Namespace };
        var failure = injected;
        if (failure is null)
        {
            try
            {
                await Invoke(claim.Payload, context);
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        }
        try
        {
            if (failure is not null)
            {
                var failed = await Wire.FailOccurrence(new() { EventStore = EventStore.Name, Namespace = EventStore.Namespace, Trigger = _trigger, OccurrenceId = claim.Id, Epoch = claim.Epoch, Error = failure.ToString() }).EnsureSuccess();
                return failed.Status switch
                {
                    OccurrenceLeaseResult.Updated => OccurrenceDeliveryOutcome.Failed,
                    OccurrenceLeaseResult.OccurrenceLeaseLost => OccurrenceDeliveryOutcome.LeaseLost,
                    _ => throw new TriggerNotValid("The kernel returned an unknown failure result.")
                };
            }
            if (!complete) return OccurrenceDeliveryOutcome.Delivered;
            var completed = await Wire.CompleteOccurrence(new() { EventStore = EventStore.Name, Namespace = EventStore.Namespace, Trigger = _trigger, OccurrenceId = claim.Id, Epoch = claim.Epoch }).EnsureSuccess();
            return completed.Status switch
            {
                OccurrenceLeaseResult.Updated or OccurrenceLeaseResult.AlreadyCompleted => OccurrenceDeliveryOutcome.Completed,
                OccurrenceLeaseResult.OccurrenceLeaseLost => OccurrenceDeliveryOutcome.LeaseLost,
                _ => throw new TriggerNotValid("The kernel returned an unknown completion result.")
            };
        }
        finally
        {
            await StatusOf(context.Id);
            _events.Clear();
            _events.AddRange(await EventStore.EventLog.GetFromSequenceNumber(EventSequenceNumber.First));
        }
    }

    async Task Invoke(string payload, OccurrenceContext context)
    {
        await using var scope = Provider.CreateAsyncScope();
        var activator = new ClientArtifactsActivator(scope.ServiceProvider, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        var activation = activator.Activate(scope.ServiceProvider, typeof(THandler));
        if (activation.TryGetException(out var exception)) throw exception;
        await using var artifact = activation.AsT0;
        var trigger = JsonSerializer.Deserialize(payload, _triggerType, Cratis.Json.Globals.JsonSerializerOptions) ?? throw new TriggerNotValid("A trigger payload must not be null.");
        try
        {
            var method = typeof(IOccurrenceHandler<>).MakeGenericType(_triggerType).GetMethod(nameof(IOccurrenceHandler<object>.Handle))!;
            await (Task)method.Invoke(artifact.Instance, [trigger, context, CancellationToken.None])!;
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
        }
    }
}
