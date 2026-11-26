// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

/// <summary>
/// Produces a duplicate pair and another event through the built-in recorder.
/// </summary>
public class AssertionReactor : IReactor
{
    /// <summary>
    /// Produces three events for each input.
    /// </summary>
    /// <param name="event">The input.</param>
    /// <returns>The event collection.</returns>
    public IEnumerable<AssertionEvent> Handle(ReactorRouteRecorded @event) => [Create(@event.Note), Create(@event.Note), Create("second")];

    /// <summary>
    /// Creates an independent but structurally equivalent event.
    /// </summary>
    /// <param name="name">The event name.</param>
    /// <returns>The event.</returns>
    public static AssertionEvent Create(string name) => new(name, [1, 2], new Dictionary<string, object?> { ["note"] = null, ["amount"] = 12m });
}
