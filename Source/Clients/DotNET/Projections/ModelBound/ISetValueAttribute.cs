// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Defines an attribute that indicates a property should be set to a constant value when an event occurs.
/// </summary>
public interface ISetValueAttribute : IEventBoundAttribute
{
    /// <summary>
    /// Gets the constant value to set, or <see langword="null"/> to clear the member back to no value.
    /// </summary>
    object? Value { get; }
}
