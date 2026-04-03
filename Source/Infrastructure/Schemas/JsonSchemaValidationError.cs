// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Represents a validation error from a JSON Schema validation.
/// </summary>
/// <param name="Path">The JSON path where the error occurred.</param>
/// <param name="Kind">The kind of validation error.</param>
/// <param name="Message">A human-readable message for the error.</param>
public record JsonSchemaValidationError(string? Path, JsonSchemaValidationErrorKind Kind, string Message)
{
    /// <inheritdoc/>
    public override string ToString() => Message;
}
