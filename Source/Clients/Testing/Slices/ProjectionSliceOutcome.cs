// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels;

namespace Cratis.Chronicle.Testing.Slices;

/// <summary>
/// Provides fluent assertions against the projection slice's live scenario state.
/// </summary>
/// <typeparam name="TReadModel">The read model type.</typeparam>
public sealed class ProjectionSliceOutcome<TReadModel>
    where TReadModel : class
{
    readonly ReadModelScenario<TReadModel> _scenario;

    /// <summary>
    /// Initializes assertions over the owned scenario.
    /// </summary>
    /// <param name="scenario">The executed scenario.</param>
    internal ProjectionSliceOutcome(ReadModelScenario<TReadModel> scenario) => _scenario = scenario;

    /// <summary>
    /// Matches listed properties at a materialized read-model key, ignoring unlisted properties.
    /// </summary>
    /// <param name="eventSourceId">The read-model key.</param>
    /// <param name="expected">The partial shape.</param>
    /// <returns>This outcome.</returns>
    /// <exception cref="ReadModelAssertionFailed">The keyed partial assertion fails.</exception>
    public ProjectionSliceOutcome<TReadModel> ShouldPartiallyMatchReadModel(EventSourceId eventSourceId, object expected) =>
        Should(scenario => scenario.ShouldPartiallyMatchReadModel(eventSourceId, expected));

    /// <summary>
    /// Matches the complete public property set at a materialized read-model key.
    /// </summary>
    /// <param name="eventSourceId">The read-model key.</param>
    /// <param name="expected">The complete shape.</param>
    /// <returns>This outcome.</returns>
    /// <exception cref="ReadModelAssertionFailed">The keyed exact assertion fails.</exception>
    public ProjectionSliceOutcome<TReadModel> ShouldExactlyMatchReadModel(EventSourceId eventSourceId, object expected) =>
        Should(scenario => scenario.ShouldExactlyMatchReadModel(eventSourceId, expected));

    /// <summary>
    /// Asserts absence at a materialized read-model key.
    /// </summary>
    /// <param name="eventSourceId">The read-model key.</param>
    /// <returns>This outcome.</returns>
    /// <exception cref="ReadModelAssertionFailed">Keyed state is unavailable or an instance exists.</exception>
    public ProjectionSliceOutcome<TReadModel> ShouldHaveNoReadModel(EventSourceId eventSourceId) =>
        Should(scenario => scenario.ShouldHaveNoReadModel(eventSourceId));

    /// <summary>
    /// Runs an advanced assertion against the scenario, propagating its failure unchanged.
    /// </summary>
    /// <param name="assertion">The assertion to execute.</param>
    /// <returns>This outcome.</returns>
    public ProjectionSliceOutcome<TReadModel> Should(Action<ReadModelScenario<TReadModel>> assertion)
    {
        assertion(_scenario);

        return this;
    }
}
