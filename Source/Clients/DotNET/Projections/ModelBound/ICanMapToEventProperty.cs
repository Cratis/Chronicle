// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Defines an attribute that can map to an event property.
/// </summary>
public interface ICanMapToEventProperty
{
    /// <summary>
    /// Gets the name of the property on the event.
    /// </summary>
    string? EventPropertyName { get; }
}
