// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Defines the kinds of JSON Schema validation errors.
/// </summary>
public enum JsonSchemaValidationErrorKind
{
    /// <summary>
    /// An unspecified validation error.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// A required property is missing.
    /// </summary>
    PropertyRequired,

    /// <summary>
    /// The value type does not match the expected type.
    /// </summary>
    WrongPropertyType,

    /// <summary>
    /// An additional property is not allowed.
    /// </summary>
    AdditionalPropertiesNotAllowed,

    /// <summary>
    /// The value does not match the expected format.
    /// </summary>
    FormatMismatch,
}
