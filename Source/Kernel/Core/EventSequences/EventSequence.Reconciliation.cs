// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Captures;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Supervision;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Reconciles the projection, capture and pattern-capture subscriptions of a namespace from its event log.
/// </summary>
/// <remarks>
/// <para>
/// The event log of a namespace is the grain that is always there once the namespace has events, so it is where the
/// namespace-wide subscriptions are put right. It does so when it activates and when a silo leaves the cluster, which
/// is what drops subscriptions. A namespace that is added is reconciled by the components that hear it
/// (their namespace-added handlers), so no grain timer runs per namespace any more.
/// </para>
/// <para>
/// A pass that fails leaves the namespace unreconciled, so the log asks the silo's <see cref="ISiloSupervisor"/> to run
/// it again - after a couple of seconds at first, then backing off towards <see cref="Configuration.Observers.WatchdogInterval"/> - until a pass finds nothing failing. A
/// healthy namespace costs nothing between departures.
/// </para>
/// </remarks>
public partial class EventSequence
{
    static readonly TimeSpan _reconcileRetryStart = TimeSpan.FromSeconds(2);

    int _reconcilesInFlight;
    bool _reconcileFailed;
    bool _retryingReconcile;
    string _supervisionKey = string.Empty;

    /// <inheritdoc/>
    public async Task ReconcileSubscriptions()
    {
        if (_eventSequenceId != EventSequenceId.Log)
        {
            return;
        }

        // Never await manager subscription here: Observer.Subscribe reads this sequence's tail.
        // Each manager coalesces pending work per namespace; failures are logged and the next pass retries.
        // All three parts are counted before any of them starts, so a part that finishes at once cannot settle the pass.
        _reconcilesInFlight += 3;
        _ = ReconcileNamespaceSubscriptions(() => GrainFactory.GetGrain<IProjectionsManager>(_eventSequenceKey.EventStore).EnsureSubscribedIn(_eventSequenceKey.Namespace));
        _ = ReconcileNamespaceSubscriptions(() => GrainFactory.GetGrain<ICapturesManager>(_eventSequenceKey.EventStore).EnsureSubscribedIn(_eventSequenceKey.Namespace));
        try
        {
            // A persisted Active marker can outlive failed setup and be reloaded by an ordinary retry.
            // Always let the observer decide from its actual state machine and setup outcome, in the same
            // serialized turn that guards quarantine and performs recovery. This stays off the append path.
            await patternCapture.RecoverSubscription(_eventSequenceKey.EventStore, _eventSequenceKey.Namespace);
        }
        catch (Exception exception)
        {
            _reconcileFailed = true;
            logger.FailedReconcilingPatternCapture(exception, _eventSequenceKey.EventStore, _eventSequenceKey.Namespace);
        }
        finally
        {
            _reconcilesInFlight--;
            SettleReconciliation();
        }
    }

    void BeginReconciliation()
    {
        if (_eventSequenceId != EventSequenceId.Log)
        {
            return;
        }

        _supervisionKey = $"event-log:{_eventSequenceKey.EventStore}:{_eventSequenceKey.Namespace}";
        RegisterReconciliation();

        // The first pass runs once, right after activation, in a turn of its own: subscribing the observer reads this
        // sequence's tail, so it must be able to interleave, and neither activation nor an append may wait on it.
        IGrainTimer? timer = null;
        timer = this.RegisterGrainTimer(
            async _ =>
            {
                timer?.Dispose();
                await ReconcileSubscriptions();
            },
            new GrainTimerCreationOptions
            {
                DueTime = TimeSpan.Zero,
                Period = Timeout.InfiniteTimeSpan,
                Interleave = true
            });
    }

    void EndReconciliation()
    {
        if (_supervisionKey.Length > 0)
        {
            supervisor?.Unregister(_supervisionKey);
        }
    }

    void RegisterReconciliation()
    {
        if (supervisor is null)
        {
            return;
        }

        var self = GrainFactory.GetGrain<IEventSequence>(this.GetPrimaryKeyString());
        var ceiling = TimeSpan.FromSeconds(Math.Max(1, options.Value.Observers.WatchdogInterval));
        var retry = _retryingReconcile ? TimeSpan.FromTicks(Math.Min(_reconcileRetryStart.Ticks, ceiling.Ticks)) : ceiling;
        var delay = retry;
        Func<Task<TimeSpan?>>? retryCheck = null;
        if (_retryingReconcile)
        {
            // A namespace whose subscriptions are not reconciled has observers that are not subscribed, so the first
            // retries are quick and back off towards the watchdog interval while the cluster is still struggling.
            retryCheck = async () =>
            {
                await self.ReconcileSubscriptions();
                delay = TimeSpan.FromTicks(Math.Min(delay.Ticks * 2, ceiling.Ticks));
                return delay;
            };
        }

        supervisor.Register(_supervisionKey, _retryingReconcile ? retry : null, retryCheck, self.ReconcileSubscriptions);
    }

    void SettleReconciliation()
    {
        if (_reconcilesInFlight > 0)
        {
            return;
        }

        var needsRetry = _reconcileFailed;
        _reconcileFailed = false;
        if (needsRetry == _retryingReconcile)
        {
            return;
        }

        _retryingReconcile = needsRetry;
        RegisterReconciliation();
    }

    async Task ReconcileNamespaceSubscriptions(Func<Task> reconcile)
    {
        try
        {
            await reconcile();
        }
        catch (Exception exception)
        {
            _reconcileFailed = true;
            logger.FailedReconcilingNamespaceSubscriptions(exception, _eventSequenceKey.EventStore, _eventSequenceKey.Namespace);
        }
        finally
        {
            _reconcilesInFlight--;
            SettleReconciliation();
        }
    }
}
