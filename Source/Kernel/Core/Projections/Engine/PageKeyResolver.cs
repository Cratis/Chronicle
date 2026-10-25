// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// Associates framework resolvers with a read-only plan. Unknown delegates are never executed during preparation.
/// </summary>
/// <param name="resolver">The unchanged runtime resolver.</param>
/// <param name="plan">The optional read-only plan.</param>
internal sealed class PageKeyResolver(KeyResolver resolver, Func<AppendedEvent, PageParentReads, Task<Key?>> plan)
{
    readonly Func<AppendedEvent, PageParentReads, Task<Key?>> _plan = plan;

    /// <summary>Associates an IO-free framework resolver with its read-only plan.</summary>
    /// <param name="resolver">The IO-free resolver.</param>
    /// <returns>The runtime resolver with preparation metadata.</returns>
    internal static KeyResolver Pure(KeyResolver resolver) => new PageKeyResolver(resolver, async (@event, _) =>
        (await resolver(null!, null!, @event) as ResolvedKey)?.Key).Resolve;

    /// <summary>Plans only the directly knowable parent path, never hierarchy fallback.</summary>
    /// <param name="resolver">The unchanged hierarchy resolver.</param>
    /// <param name="projection">The child projection.</param>
    /// <param name="parentKeyResolver">The resolver for the parent source.</param>
    /// <param name="constant">Whether the parent key names the root directly.</param>
    /// <returns>The runtime resolver with preparation metadata.</returns>
    internal static KeyResolver Parent(KeyResolver resolver, IProjection projection, KeyResolver parentKeyResolver, bool constant) =>
        new PageKeyResolver(resolver, async (@event, reads) =>
        {
            using var scope = ParentKeyResolutionScope.ForChild(@event.Context.SequenceNumber);
            var parentKey = await Plan(parentKeyResolver, @event, reads);
            if (parentKey?.Value is null) return null;
            var parent = projection.HasParent ? projection.Parent! : projection;
            var types = parent.OwnEventTypes.Select(type => type.Id).ToArray();
            if (!projection.HasParent || constant || types.Length == 0) return parentKey;

            // Creation-event fallback, joins and sink-based hierarchy discovery remain on the normal resolver path.
            var parentEvent = types.Contains(@event.Context.EventType.Id)
                ? @event
                : reads.Find(parentKey.Value.ToString()!, types);
            if (parentEvent is null) return null;
            var pin = parent.EventTypes.FirstOrDefault(type => type.Id == parentEvent.Context.EventType.Id);
            if (pin is null || (parent.GenerationDelivery == EventGenerationDelivery.Pinned && pin.Generation != parentEvent.Context.EventType.Generation)) return null;
            return await Plan(parent.GetKeyResolverFor(pin), parentEvent, reads);
        }).Resolve;

    /// <summary>Plans a known resolver without executing unknown delegates.</summary>
    /// <param name="resolver">The resolver to inspect.</param>
    /// <param name="event">The event being planned.</param>
    /// <param name="reads">This page's parent reads.</param>
    /// <returns>A root candidate when it can be known without further IO.</returns>
    internal static Task<Key?> Plan(KeyResolver resolver, AppendedEvent @event, PageParentReads reads) =>
        resolver.Target is PageKeyResolver preparable ? preparable._plan(@event, reads) : Task.FromResult<Key?>(null);

    Task<KeyResolverResult> Resolve(IEventSequenceStorage storage, ISink sink, AppendedEvent @event) => resolver(storage, sink, @event);
}
