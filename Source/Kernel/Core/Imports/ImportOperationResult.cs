// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Imports;

namespace Cratis.Chronicle.Imports;

/// <summary>
/// Represents the outcome of an operation on an import session.
/// </summary>
/// <param name="Record">The <see cref="ImportRecord"/> after the operation, when the import exists.</param>
/// <param name="Error">Why the operation was refused, when it was.</param>
public record ImportOperationResult(ImportRecord? Record, string? Error = default)
{
    /// <summary>
    /// Gets a value indicating whether the operation was carried out.
    /// </summary>
    public bool IsSuccess => Error is null;

    /// <summary>
    /// Creates a refused result.
    /// </summary>
    /// <param name="record">The record as it stands.</param>
    /// <param name="error">Why.</param>
    /// <returns>The <see cref="ImportOperationResult"/>.</returns>
    public static ImportOperationResult Refused(ImportRecord? record, string error) => new(record, error);
}
