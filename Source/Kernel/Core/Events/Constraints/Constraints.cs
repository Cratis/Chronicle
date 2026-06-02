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
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.Constraints)]
public class Constraints(IClusterClient clusterClient) : Grain<ConstraintsState>, IConstraints
{
    readonly IBroadcastChannelProvider _constraintsChangedChannel = clusterClient.GetBroadcastChannelProvider(WellKnownBroadcastChannelNames.ConstraintsChanged);

    /// <inheritdoc/>
    public async Task Register(IEnumerable<IConstraintDefinition> definitions)
    {
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
            await ConstraintsChanged(changes);
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
