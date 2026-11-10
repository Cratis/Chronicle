// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Defines an attribute that indicates that a property value should be set from an event context property.
/// </summary>
public interface ISetFromContextAttribute : IEventBoundAttribute
{
    /// <summary>
    /// Gets the name of the property on the event context.
    /// </summary>
    string? ContextPropertyName { get; }

    /// <summary>
    /// Gets a value indicating whether the property is only assigned when it has no value yet.
    /// </summary>
    bool SetOnce => false;
}
