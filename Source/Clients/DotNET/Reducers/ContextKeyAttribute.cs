// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Reducers;

/// <summary>
/// Represents metadata for defining which property on the event to use as key.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class ContextKeyAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KeyAttribute"/> class.
    /// </summary>
    /// <param name="property">Property to use.</param>
    public ContextKeyAttribute(string property)
    {
        Property = property;
    }

    /// <summary>
    /// Gets the property to use as key.
    /// </summary>
    public string Property { get; }
}
