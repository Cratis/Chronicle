// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Imports;

namespace Cratis.Chronicle.Imports;

/// <summary>
/// Represents an import session as begun, with the limits the server puts on its chunks.
/// </summary>
/// <param name="ImportId">The identifier of the import.</param>
/// <param name="State">The state of the import.</param>
/// <param name="MaxChunkEvents">The most events one chunk may carry.</param>
/// <param name="MaxChunkBytes">The most bytes of event content one chunk may carry.</param>
/// <param name="Error">Why the import could not be begun, when it could not.</param>
public record ImportSession(Guid ImportId, ImportState State, int MaxChunkEvents, int MaxChunkBytes, string? Error = default);
