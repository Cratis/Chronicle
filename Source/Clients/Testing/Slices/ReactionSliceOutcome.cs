// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.Reactors;

namespace Cratis.Chronicle.Testing.Slices;

/// <summary>
/// Provides fluent assertions against the reaction slice's cumulative recording and dependencies.
/// </summary>
/// <typeparam name="TReactor">The reactor type.</typeparam>
public sealed class ReactionSliceOutcome<TReactor>
    where TReactor : class
{
    readonly ReactorScenario<TReactor> _scenario;

    /// <summary>
    /// Initializes assertions over the owned scenario.
    /// </summary>
    /// <param name="scenario">The executed scenario.</param>
    internal ReactionSliceOutcome(ReactorScenario<TReactor> scenario) => _scenario = scenario;

    /// <summary>
    /// Matches the complete typed event list in order, ignoring destinations and commands.
    /// </summary>
    /// <param name="events">The ordered expected events.</param>
    /// <returns>This outcome.</returns>
    /// <exception cref="ReactorSideEffectAssertionException">The recorded events differ.</exception>
    public ReactionSliceOutcome<TReactor> ShouldHaveProducedEventsInOrder(params object[] events) =>
        Should(scenario => scenario.ShouldHaveProducedEventsInOrder(events));

    /// <summary>
    /// Matches the complete typed event multiset, retaining duplicate counts.
    /// </summary>
    /// <param name="events">The expected events in any order.</param>
    /// <returns>This outcome.</returns>
    /// <exception cref="ReactorSideEffectAssertionException">Events are missing or unexpected.</exception>
    public ReactionSliceOutcome<TReactor> ShouldHaveProducedEventsInAnyOrder(params object[] events) =>
        Should(scenario => scenario.ShouldHaveProducedEventsInAnyOrder(events));

    /// <summary>
    /// Asserts that no events were recorded, without asserting that no commands or external effects occurred.
    /// </summary>
    /// <returns>This outcome.</returns>
    /// <exception cref="ReactorSideEffectAssertionException">Events were recorded.</exception>
    public ReactionSliceOutcome<TReactor> ShouldNotHaveProducedEvents() => Should(scenario => scenario.ShouldNotHaveProducedEvents());

    /// <summary>
    /// Runs an advanced destination, command, or injected side-effect assertion, propagating failures unchanged.
    /// </summary>
    /// <param name="assertion">The assertion to execute.</param>
    /// <returns>This outcome.</returns>
    public ReactionSliceOutcome<TReactor> Should(Action<ReactorScenario<TReactor>> assertion)
    {
        assertion(_scenario);

        return this;
    }
}
