// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// The exception that is thrown when one or more projection definitions in a registration failed to register,
/// while the rest of the registration was applied.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SomeProjectionDefinitionsFailedToRegister"/> class.
/// </remarks>
/// <param name="eventStore">The <see cref="EventStoreName"/> the registration was for.</param>
/// <param name="failures">The failure per <see cref="ProjectionId"/> that did not register.</param>
[GenerateSerializer]
public class SomeProjectionDefinitionsFailedToRegister(
    EventStoreName eventStore,
    IReadOnlyDictionary<ProjectionId, Exception> failures) : Exception(
        $"Failed to register projection(s) {string.Join(", ", failures.Keys.Select(id => $"'{id}'"))} for event store " +
        $"'{eventStore}'. Every other projection in the registration was applied. First failure: {failures.Values.First().Message}",
        failures.Values.First())
{
    /// <summary>
    /// Gets the failure per <see cref="ProjectionId"/> that did not register.
    /// </summary>
    [Id(0)]
    public IReadOnlyDictionary<ProjectionId, Exception> Failures { get; } = failures;
}
