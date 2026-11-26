// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.given;

/// <summary>
/// A recursive public shape for read-model assertion specifications.
/// </summary>
/// <param name="Name">The document name.</param>
/// <param name="Amount">A typed scalar.</param>
/// <param name="Values">An ordered collection.</param>
/// <param name="Metadata">Dictionary entries with nested values.</param>
public record AssertionDocument(string Name, decimal Amount, IReadOnlyList<int> Values, IReadOnlyDictionary<string, JoinCustomerId?> Metadata)
{
    /// <summary>
    /// Gets or sets the optional next document, including cyclic references.
    /// </summary>
    public AssertionDocument? Next { get; set; }
}
