// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Projects in-memory events, retaining deferred children until their parent can release them.
/// </summary>
/// <param name="projection">The projection whose keys resolve against the state built so far.</param>
/// <param name="storage">The event sequence storage.</param>
/// <param name="sink">The in-memory read models.</param>
/// <param name="project">Projects an event with its resolved key.</param>
internal sealed class InMemoryProjectionEvents(
    Engine.IProjection projection,
    IEventSequenceStorage storage,
    ISink sink,
    Func<ResolvedKey, AppendedEvent, Task> project)
{
    readonly List<AppendedEvent> _deferred = [];

    /// <summary>
    /// Projects an event and retries pending children against the releasing event's inclusive history boundary.
    /// </summary>
    /// <param name="event">The event to project.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task Project(AppendedEvent @event)
    {
        var result = await Resolve(@event);
        if (result is DeferredKey)
        {
            _deferred.Add(@event);
            return;
        }

        if (result is ResolvedKey resolvedKey)
        {
            await project(resolvedKey, @event);
            await ReleaseDeferred(@event.Context.SequenceNumber);
        }
    }

    Task<KeyResolverResult> Resolve(AppendedEvent @event) =>
        projection.GetKeyResolverFor(@event.Context.EventType)(storage, sink, @event);

    async Task ReleaseDeferred(EventSequenceNumber sequenceNumber)
    {
        using var scope = ParentKeyResolutionScope.ForDeferredRelease(sequenceNumber);
        bool resolvedAny;
        do
        {
            resolvedAny = false;
            foreach (var pending in _deferred.ToArray())
            {
                var result = await Resolve(pending);
                if (result is ResolvedKey resolvedKey)
                {
                    await project(resolvedKey, pending);
                    _deferred.Remove(pending);
                    resolvedAny = true;
                }
                else if (result is UnresolvableKey)
                {
                    _deferred.Remove(pending);
                }
            }
        }
        while (resolvedAny);
    }
}
