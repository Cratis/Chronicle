// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Defines an attribute that indicates that a property value should be added from an event property.
/// </summary>
public interface IAddFromAttribute : IEventBoundAttribute
{
    /// <summary>
    /// Gets the name of the property on the event.
    /// </summary>
    string? EventPropertyName { get; }
}
