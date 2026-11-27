// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.ExceptionServices;

namespace Cratis.Chronicle.Storage.Sinks;

public sealed partial class SinkProvenanceCache
{
    internal sealed partial class Entry
    {
        readonly HashSet<ISinkProvenanceStorage> _stores = new(ReferenceEqualityComparer.Instance);
        long _revision;

        /// <summary>
        /// Determines whether a route observation agrees with the others and has not lost completion.
        /// </summary>
        /// <param name="state">The route observation.</param>
        /// <param name="previous">An earlier observation in the same pass, if any.</param>
        /// <param name="minimumVersion">The lowest inventory version that may be accepted.</param>
        /// <returns>True when the observation is consistent.</returns>
        internal static bool HasConsistentInventory(SinkProvenanceState state, SinkProvenanceState? previous, int minimumVersion) =>
            state.InventoryVersion >= minimumVersion && (previous is null || previous.InventoryVersion == state.InventoryVersion);

        /// <summary>
        /// Registers a route and applies its validated initial observation.
        /// </summary>
        /// <param name="store">The route's durable metadata access.</param>
        /// <param name="state">The observed state.</param>
        /// <param name="started">The monotonic start of the observation.</param>
        internal void Register(ISinkProvenanceStorage store, SinkProvenanceState state, long started)
        {
            lock (_gate)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                if (_stores.Add(store)) ApplyRouteObservation(state, started);
                else if (state.Restricted) LatchRestriction();
            }
            if (state.Restricted) PublishRevocation();
        }

        /// <summary>
        /// Applies a validated completion observation from an individual route.
        /// </summary>
        /// <param name="state">The committed state.</param>
        /// <param name="started">The monotonic start of the operation.</param>
        /// <exception cref="InvalidOperationException">The observation changed database identity or lost completion.</exception>
        internal void ObserveCompletion(SinkProvenanceState state, long started)
        {
            bool accepted;
            lock (_gate)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                accepted = ApplyRouteObservation(state, started);
            }
            if (state.DatabaseId == _databaseId && state.Restricted) PublishRevocation();
            if (!accepted) throw SinkProvenanceLease.UnavailableError();
        }

        bool ApplyRouteObservation(SinkProvenanceState state, long started)
        {
            _revision++;
            if (state.DatabaseId != _databaseId)
            {
                Invalidate();
                return false;
            }

            if (state.Restricted) LatchRestriction();
            var accepted = state.InventoryVersion >= _state.InventoryVersion;
            if (accepted && _stores.Count == 1) PublishState(state, started);
            else Invalidate();
            return accepted;
        }

        RefreshSnapshot SnapshotRoutes(TimeProvider clock)
        {
            lock (_gate)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                return new([.. _stores], _revision, clock.GetTimestamp(), _state.InventoryVersion);
            }
        }

        async Task<SinkProvenanceState> ReadRoutes(RefreshSnapshot snapshot)
        {
            SinkProvenanceState? observation = null;
            Exception? failure = null;
            foreach (var store in snapshot.Stores)
            {
                try
                {
                    var state = await ReadRoute(store).ConfigureAwait(false);
                    if (!HasConsistentInventory(state, observation, snapshot.InventoryVersion)) throw SinkProvenanceLease.UnavailableError();
                    observation ??= state;
                }
                catch (Exception exception)
                {
                    InvalidateRefresh(snapshot.Revision);
                    failure ??= exception;
                }
            }

            if (failure is not null) ExceptionDispatchInfo.Throw(failure);
            return observation ?? throw SinkProvenanceLease.UnavailableError();
        }

        async Task<SinkProvenanceState> ReadRoute(ISinkProvenanceStorage store)
        {
            var state = (await store.ReadState().ConfigureAwait(false)).Validate();
            if (state.DatabaseId != _databaseId) throw SinkProvenanceLease.UnavailableError();
            if (state.Restricted) Deny();
            return state;
        }

        void PublishRefresh(RefreshSnapshot snapshot, SinkProvenanceState state)
        {
            lock (_gate)
            {
                if (!IsCurrent(snapshot.Revision)) throw SinkProvenanceLease.UnavailableError();
                PublishState(state, snapshot.Started);
            }
        }

        void InvalidateRefresh(long revision)
        {
            lock (_gate)
            {
                if (IsCurrent(revision)) Invalidate();
            }
        }

        bool IsCurrent(long revision) => !_disposed && _revision == revision;

        void PublishState(SinkProvenanceState state, long started)
        {
            _state = state with { Restricted = _state.Restricted || state.Restricted };
            _started = started;
            _valid = true;
            Pulse();
        }

        void Invalidate()
        {
            _valid = false;
            Pulse();
        }

        void LatchRestriction()
        {
            if (_state.Restricted) return;
            _state = _state with { Restricted = true };
            Pulse();
        }

        sealed record RefreshSnapshot(ISinkProvenanceStorage[] Stores, long Revision, long Started, int InventoryVersion);
    }
}
