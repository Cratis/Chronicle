// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.V2;

/// <summary>
/// Carries one schema-converted property in declaration order.
/// </summary>
/// <param name="Value">The scalar, scalar collection, or null for a null or missing property.</param>
/// <param name="Format">The declared scalar format, or the element format for a collection.</param>
/// <param name="IsInSchema">Whether the property is declared in the schema.</param>
/// <remarks>
/// A formatless string stays a string unless the property is outside the schema.
/// Callers supply schema-typed CLR values, not JSON literals. This component does not perform schema conversion.
/// </remarks>
internal record UniqueConstraintComponent(object? Value, string? Format = null, bool IsInSchema = true);
