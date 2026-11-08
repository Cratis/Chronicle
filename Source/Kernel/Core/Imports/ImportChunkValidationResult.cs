// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Imports;

namespace Cratis.Chronicle.Imports;

/// <summary>
/// Represents the result of validating one chunk of an import against the constraints.
/// </summary>
/// <param name="Rejections">The events of the chunk that would break a constraint.</param>
/// <param name="MustRestart">True when the import's claims are gone or an index is unavailable, and validation must start over.</param>
public record ImportChunkValidationResult(IEnumerable<ImportRejection> Rejections, bool MustRestart = false)
{
    /// <summary>
    /// Gets the result telling the validation to start over from the first chunk.
    /// </summary>
    public static readonly ImportChunkValidationResult Restart = new([], true);
}
