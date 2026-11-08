// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.Reactors;

/// <summary>
/// Represents the entry point of the fluent builder for providing events to a <see cref="ReactorScenario{TReactor}"/>.
/// </summary>
/// <typeparam name="TReactor">The type of reactor under test.</typeparam>
/// <param name="scenario">The <see cref="ReactorScenario{TReactor}"/> to drive.</param>
public class ReactorScenarioGivenBuilder<TReactor>(ReactorScenario<TReactor> scenario)
    where TReactor : class
{
    /// <summary>
    /// Specifies the <see cref="EventSourceId"/> that the events originate from.
    /// </summary>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> to associate with the events.</param>
    /// <returns>A <see cref="ReactorSourceGivenBuilder{TReactor}"/> to continue the fluent chain.</returns>
    public ReactorSourceGivenBuilder<TReactor> ForEventSource(EventSourceId eventSourceId) =>
        new(scenario, eventSourceId);

    /// <summary>
    /// Specifies the <see cref="EventSourceId"/> to associate a seeded read model instance (or events) with.
    /// </summary>
    /// <remarks>
    /// Use <c language="csharp">.ReadModel(...)</c> on the returned builder to seed a read-model handler-method parameter of the
    /// reactor, or <c language="csharp">.Events(...)</c> to drive events.
    /// </remarks>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> to seed for.</param>
    /// <returns>A <see cref="ReactorSourceGivenBuilder{TReactor}"/> to continue the fluent chain.</returns>
    public ReactorSourceGivenBuilder<TReactor> ForEventSourceId(EventSourceId eventSourceId) =>
        new(scenario, eventSourceId);

    /// <summary>
    /// Delivers events of several event sources together, as one live page.
    /// </summary>
    /// <remarks>
    /// Each batch method of the reactor is called once with the events of the types it observes, across event sources,
    /// in the order given. Events no batch method observes reach the per-event methods one at a time.
    /// </remarks>
    /// <param name="events">The events and the event sources they are for, in the order they are appended.</param>
    /// <returns>A <see cref="Task"/> that completes when every method has handled its events.</returns>
    public Task Page(params EventForEventSourceId[] events) => scenario.InvokeWithPage(events);
}
