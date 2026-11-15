// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Imports;

namespace Cratis.Chronicle.Imports;

/// <summary>
/// Represents the result of validating one chunk of an import against the constraints.
/// </summary>
/// <param name="Rejections">The events of the chunk that would break a constraint.</param>
/// <param name="MustRestart">True when the import's claims are gone or an index is unavailable, and validation must start over.</param>
/// <param name="MustWait">True when the event sequence already validates as many imports as it keeps claims for; the import validates nothing yet and starts again later.</param>
public record ImportChunkValidationResult(IEnumerable<ImportRejection> Rejections, bool MustRestart = false, bool MustWait = false)
{
    /// <summary>
    /// Gets the result telling the validation to start over from the first chunk.
    /// </summary>
    public static readonly ImportChunkValidationResult Restart = new([], true);

    /// <summary>
    /// Gets the result telling the validation to wait for room and then start from the first chunk.
    /// </summary>
    public static readonly ImportChunkValidationResult Wait = new([], MustWait: true);
}
