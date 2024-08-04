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
        // TODO: Check for change; any new or replaced definitions should be re-initialized (Job)
        var existing = State.Constraints.Where(existing => definitions.Any(d => d.Name == existing.Name)).ToArray();
        existing.ForEach(c => State.Constraints.Remove(c));
        definitions.ForEach(State.Constraints.Add);
        await WriteStateAsync();
        await ConstraintsChanged();
    }

    async Task ConstraintsChanged()
    {
        var channelId = ChannelId.Create(WellKnownBroadcastChannelNames.ConstraintsChanged, this.GetPrimaryKeyString());
        var channelWriter = _constraintsChangedChannel.GetChannelWriter<ConstraintsChanged>(channelId);
        await channelWriter.Publish(new ConstraintsChanged());
    }
}
