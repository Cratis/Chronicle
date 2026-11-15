// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Imports;

/// <summary>
/// Represents the result of committing one chunk of an import.
/// </summary>
/// <param name="Outcomes">The outcome of every event in the chunk, when the chunk settled.</param>
/// <param name="Errors">Why the chunk could not settle; empty when it did. A chunk that did not settle is tried again.</param>
public record ImportChunkAppendResult(IEnumerable<ImportEventOutcome> Outcomes, IEnumerable<string> Errors)
{
    /// <summary>
    /// Gets a value indicating whether every event of the chunk settled.
    /// </summary>
    public bool IsSettled => !Errors.Any();

    /// <summary>
    /// Creates a result for a chunk that could not settle.
    /// </summary>
    /// <param name="errors">Why.</param>
    /// <returns>The <see cref="ImportChunkAppendResult"/>.</returns>
    public static ImportChunkAppendResult NotSettled(params string[] errors) => new([], errors);
}
