// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Collections;
using Orleans.BroadcastChannel;
using Orleans.Providers;

namespace Cratis.Chronicle.Grains.Events.Constraints;

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
        var existing = State.Constraints.Where(existing => definitions.Any(d => d.Name == existing.Name)).ToArray();
        var newDefinitions = definitions.Where(d => existing.All(existing => d.Name != existing.Name)).ToArray();
        var changed = existing.Join(definitions, e => e.Name, d => d.Name, (e, d) => new { Existing = e, New = d })
                      .Where(pair => !pair.Existing.Equals(pair.New))
                      .Select(pair => pair.New)
                      .ToArray();

        var hasChanges = newDefinitions.Length > 0 || changed.Length > 0;

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
            await ConstraintsChanged();
        }
    }

    async Task ConstraintsChanged()
    {
        var channelId = ChannelId.Create(WellKnownBroadcastChannelNames.ConstraintsChanged, this.GetPrimaryKeyString());
        var channelWriter = _constraintsChangedChannel.GetChannelWriter<ConstraintsChanged>(channelId);
        await channelWriter.Publish(new ConstraintsChanged());
    }
}
