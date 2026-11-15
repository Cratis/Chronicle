// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Reads the committed event store subscription definitions of the target event store.
/// </summary>
/// <param name="storage">The storage.</param>
[Singleton]
public class FederationSubscriptions(IStorage storage) : IFederationSubscriptions
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<FederationSubscription>> Get(FederationBindingState binding)
    {
        var definitions = await storage.GetEventStore(new EventStoreName(binding.TargetEventStore)).EventStoreSubscriptions.GetAll();
        return
        [
            .. definitions
                .Where(_ => _.SourceEventStore.Value == binding.LogicalSource)
                .Select(_ => new FederationSubscription([.. _.EventTypes.Select(type => type.Id.Value)], [.. _.NamedTags]))
        ];
    }
}
