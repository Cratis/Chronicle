// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Keys;

/// <summary>
/// Represents metadata for defining which property on the event to use as key.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="KeyAttribute"/> class.
/// </remarks>
/// <param name="property">Property to use.</param>
[AttributeUsage(AttributeTargets.Method)]
public sealed class KeyAttribute(string property) : Attribute
{
    /// <summary>
    /// Gets the property to use as key.
    /// </summary>
    public string Property { get; } = property;
}
