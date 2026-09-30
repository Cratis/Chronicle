// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Collections;
using Microsoft.Extensions.Logging;
using Orleans.BroadcastChannel;
using Orleans.Providers;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Represents an implementation of <see cref="IConstraints"/>.
/// </summary>
/// <param name="clusterClient">The <see cref="IClusterClient"/> to use.</param>
/// <param name="constraintIndexes">The <see cref="IConstraintIndexes"/> for rebuilding indexes a change has made stale.</param>
/// <param name="logger">The <see cref="ILogger"/> for logging.</param>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.Constraints)]
public class Constraints(IClusterClient clusterClient, IConstraintIndexes constraintIndexes, ILogger<Constraints> logger) : Grain<ConstraintsState>, IConstraints
{
    readonly IBroadcastChannelProvider _constraintsChangedChannel = clusterClient.GetBroadcastChannelProvider(WellKnownBroadcastChannelNames.ConstraintsChanged);
    IReadOnlyCollection<IConstraintDefinition> _persisted = [];
    ConstraintsVersion? _version;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        SnapshotPersisted();
        return base.OnActivateAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyCollection<IConstraintDefinition>> GetDefinitions() => Task.FromResult(_persisted);

    /// <inheritdoc/>
    public Task<ConstraintsVersion> GetVersion() =>
        Task.FromResult(_version ??= ConstraintDefinitionComparison.ComputeVersion(_persisted));

    /// <inheritdoc/>
    /// <remarks>
    /// When the registration changes anything, the indexes it has made stale are rebuilt for every event sequence in
    /// every namespace of the event store, independently of which event sequence grains happen to be active. The
    /// definitions are persisted first, because the reindex reads them from storage.
    /// </remarks>
    public async Task Register(IEnumerable<IConstraintDefinition> definitions)
    {
        var previous = State.Constraints.ToArray();
        var definitionsArray = definitions.ToArray();
        var existing = State.Constraints.Where(current => definitionsArray.Any(d => d.Name == current.Name)).ToArray();
        var newDefinitions = definitionsArray.Where(d => existing.All(current => d.Name != current.Name)).ToArray();
        var changedPairs = existing.Join(definitionsArray, e => e.Name, d => d.Name, (e, d) => (Existing: e, New: d))
            .Where(pair => !pair.Existing.Equals(pair.New))
            .ToArray();
        var changed = changedPairs.Select(pair => pair.New).ToArray();

        var hasChanges = newDefinitions.Length > 0 || changed.Length > 0;
        var changes = GetConstraintDefinitionChanges(newDefinitions, changedPairs);

        if (newDefinitions.Length > 0)
        {
            newDefinitions.ForEach(State.Constraints.Add);
        }

        if (changed.Length > 0)
        {
            var updatedConstraints = State.Constraints
                .Where(existing => !changed.Any(c => c.Name == existing.Name))
                .Concat(changed)
                .ToList();

            State.Constraints.Clear();
            updatedConstraints.ForEach(State.Constraints.Add);
        }

        if (hasChanges)
        {
            try
            {
                await WriteStateAsync();
            }
            catch
            {
                // Nothing was persisted, so neither is anything rebuilt - a reindex reads the definitions from
                // storage. The in-memory definitions go back to the persisted ones, or a retried registration would
                // find nothing changed and never persist or rebuild at all.
                State.Constraints.Clear();
                previous.ForEach(State.Constraints.Add);
                throw;
            }

            SnapshotPersisted();

            // From here on the definitions are persisted, and a retried registration would find nothing changed. So
            // nothing may prevent the rebuild from starting, and nothing may fail the registration.
            var eventStore = ConstraintsKey.Parse(this.GetPrimaryKeyString()).EventStore;
            try
            {
                await ConstraintsChanged(changes);
            }
            catch (Exception ex)
            {
                logger.FailedPublishingConstraintsChanged(eventStore, ex);
            }

            try
            {
                await constraintIndexes.RebuildStaleIndexes(eventStore, previous, _persisted);
            }
            catch (Exception ex)
            {
                logger.FailedStartingRebuildOfStaleIndexes(eventStore, ex);
            }
        }
    }

    static List<ConstraintDefinitionChange> GetConstraintDefinitionChanges(
        IEnumerable<IConstraintDefinition> newDefinitions,
        IEnumerable<(IConstraintDefinition Existing, IConstraintDefinition New)> changedPairs)
    {
        var changes = new List<ConstraintDefinitionChange>();

        foreach (var definition in newDefinitions)
        {
            var requiresReindex = definition is UniqueConstraintDefinition;
            IReadOnlyCollection<ConstraintChangeType> changeTypes = requiresReindex
                ? [ConstraintChangeType.EventAdded, ConstraintChangeType.IndexedPropertiesChanged]
                : [ConstraintChangeType.None];

            changes.Add(new ConstraintDefinitionChange(definition.Name, requiresReindex, changeTypes));
        }

        foreach (var pair in changedPairs)
        {
            var change = pair.New.CompareWith(pair.Existing) ?? ConstraintChange.None;
            changes.Add(new ConstraintDefinitionChange(pair.New.Name, change.RequiresReindex, change.ChangeTypes));
        }

        return changes;
    }

    async Task ConstraintsChanged(IReadOnlyCollection<ConstraintDefinitionChange> changes)
    {
        var channelId = ChannelId.Create(WellKnownBroadcastChannelNames.ConstraintsChanged, this.GetPrimaryKeyString());
        var channelWriter = _constraintsChangedChannel.GetChannelWriter<ConstraintsChanged>(channelId);
        await channelWriter.Publish(new ConstraintsChanged(changes));
    }

    /// <summary>
    /// Capture the definitions as they are persisted, and let their version be derived from them.
    /// </summary>
    /// <remarks>
    /// Reading the definitions and the version interleaves with a registration in progress, so an event sequence
    /// appending meanwhile is never blocked behind it - and a registration that refreshes those sequences is never
    /// blocked by them in turn. What they are served is therefore captured only once the definitions are persisted: a
    /// reader must never see a version whose definitions storage does not hold yet, or it would cache stale validators
    /// behind a current version.
    /// </remarks>
    void SnapshotPersisted()
    {
        _persisted = State.Constraints.ToArray();
        _version = null;
    }
}
