// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.given;

/// <summary>
/// Keeps seeded documents available through the real reducer-backed scenario.
/// </summary>
public class AssertionDocumentReducer : IReducerFor<AssertionDocument>
{
    /// <summary>
    /// Gets the reducer identifier.
    /// </summary>
    public ReducerId Id => "c4cc4414-2775-426b-88cd-3bf8e5a308b0";

    /// <summary>
    /// Retains the seeded document.
    /// </summary>
    /// <param name="event">The triggering event.</param>
    /// <param name="current">The seeded document.</param>
    /// <returns>The current document.</returns>
    public AssertionDocument Retain(Tallied @event, AssertionDocument? current) => current ?? new("Created", 0m, [], new Dictionary<string, JoinCustomerId?>());
}
