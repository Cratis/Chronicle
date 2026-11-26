// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

/// <summary>
/// Produces nested collections with container types independent of their payload values.
/// </summary>
public class ContainerAssertionReactor : IReactor
{
    /// <summary>
    /// Produces an event containing a list, dictionary, and LINQ iterator.
    /// </summary>
    /// <param name="event">The triggering input.</param>
    /// <returns>The event payload.</returns>
    public Task<AssertionEvent> Handle(ReactorRouteRecorded @event) => Task.FromResult(new AssertionEvent(
        @event.Note,
        new List<int> { 1, 2 },
        new Dictionary<string, object?> { ["sequence"] = Enumerable.Range(1, 2).Select(value => value * 2) }));
}
