// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Attribute that can be applied to a property to indicate it should be projected from all event types.
/// </summary>
/// <param name="contextProperty">Optional context property to use for the mapping (e.g., "type.id"). If not specified, tries to match event property by name.</param>
/// <param name="property">Optional event property to use for the mapping. If not specified and contextProperty is not specified, tries to match by name.</param>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class FromAllAttribute(string? contextProperty = default, string? property = default) : Attribute, IProjectionAnnotation
{
    /// <summary>
    /// Gets the optional context property to map from.
    /// </summary>
    public string? ContextProperty { get; } = contextProperty;

    /// <summary>
    /// Gets the optional event property to map from.
    /// </summary>
    public string? Property { get; } = property;
}
