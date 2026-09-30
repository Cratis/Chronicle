// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Collections;
using Orleans.BroadcastChannel;
using Orleans.Providers;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Represents an implementation of <see cref="IConstraints"/>.
/// </summary>
/// <param name="clusterClient">The <see cref="IClusterClient"/> to use.</param>
/// <param name="constraintIndexes">The <see cref="IConstraintIndexes"/> for rebuilding indexes a change has made stale.</param>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.Constraints)]
public class Constraints(IClusterClient clusterClient, IConstraintIndexes constraintIndexes) : Grain<ConstraintsState>, IConstraints
{
    readonly IBroadcastChannelProvider _constraintsChangedChannel = clusterClient.GetBroadcastChannelProvider(WellKnownBroadcastChannelNames.ConstraintsChanged);
    ConstraintsVersion? _version;

    /// <inheritdoc/>
    public Task<IReadOnlyCollection<IConstraintDefinition>> GetDefinitions() =>
        Task.FromResult<IReadOnlyCollection<IConstraintDefinition>>(State.Constraints.ToArray());

    /// <inheritdoc/>
    public Task<ConstraintsVersion> GetVersion() =>
        Task.FromResult(_version ??= ConstraintDefinitionComparison.ComputeVersion(State.Constraints));

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
            await WriteStateAsync();
            _version = null;
            await ConstraintsChanged(changes);
            await constraintIndexes.RebuildStaleIndexes(ConstraintsKey.Parse(this.GetPrimaryKeyString()).EventStore, previous, State.Constraints.ToArray());
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
}
