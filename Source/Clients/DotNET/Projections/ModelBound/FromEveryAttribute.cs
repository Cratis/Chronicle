// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Attribute used to indicate that a property value should be set from every event.
/// Can either map from an event property or an event context property.
/// </summary>
/// <param name="property">Optional name of the property on the event. If not specified, uses the model property name.</param>
/// <param name="contextProperty">Optional name of the property on the event context. If not specified, uses the model property name.</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class FromEveryAttribute(string? property = default, string? contextProperty = default) : Attribute, IProjectionAnnotation
{
    /// <summary>
    /// Gets or sets the name of the property on the event (if mapping from event property).
    /// </summary>
    public string? Property { get; set; } = property;

    /// <summary>
    /// Gets or sets the name of the event context property (if mapping from event context).
    /// </summary>
    public string? ContextProperty { get; set; } = contextProperty;
}
