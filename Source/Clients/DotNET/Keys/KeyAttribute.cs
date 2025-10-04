// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Keys;

/// <summary>
/// Represents metadata for defining which property use as key, or marks a property as a key.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="KeyAttribute"/> class.
/// </remarks>
/// <param name="property">Property to use when applied to methods. Optional when applied to properties.</param>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
public sealed class KeyAttribute(string property = "") : Attribute
{
    /// <summary>
    /// Gets the property to use as key.
    /// </summary>
    public string Property { get; } = property;
}
