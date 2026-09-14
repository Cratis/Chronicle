// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration;

/// <summary>
/// Represents what a projection does to a property when an event arrives.
/// </summary>
public enum ProjectionOperation
{
    /// <summary>
    /// Replaces the property's value.
    /// </summary>
    Set = 0,

    /// <summary>
    /// Adds to the property's value.
    /// </summary>
    Add = 1,

    /// <summary>
    /// Subtracts from the property's value.
    /// </summary>
    Subtract = 2,

    /// <summary>
    /// Increases the property by one.
    /// </summary>
    Increment = 3,

    /// <summary>
    /// Decreases the property by one.
    /// </summary>
    Decrement = 4,

    /// <summary>
    /// Counts the events that reach the property.
    /// </summary>
    Count = 5,

    /// <summary>
    /// Clears the property's value.
    /// </summary>
    Clear = 6
}
