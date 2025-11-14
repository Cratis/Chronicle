// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Defines an attribute that indicates a property should be populated through a join with an event.
/// </summary>
public interface IJoinAttribute : IEventBoundAttribute, ICanMapToEventProperty
{
    /// <summary>
    /// Gets the property name on the model to join on.
    /// </summary>
    string? On { get; }
}
