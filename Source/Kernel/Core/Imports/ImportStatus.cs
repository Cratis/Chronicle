// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Imports;

namespace Cratis.Chronicle.Imports;

/// <summary>
/// Represents the state of an import after an operation on it.
/// </summary>
/// <param name="ImportId">The identifier of the import.</param>
/// <param name="State">The state of the import.</param>
/// <param name="Error">Why the operation was refused, when it was.</param>
public record ImportStatus(Guid ImportId, ImportState State, string? Error = default);
