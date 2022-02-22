// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Properties;

/// <summary>
/// Exception that gets thrown when a children property is not enumerable for a specific type.
/// </summary>
public class ChildrenPropertyIsNotEnumerableForType : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChildrenPropertyIsNotEnumerable"/> class.
    /// </summary>
    /// <param name="type">Type that is the root of the <see cref="PropertyPath"/>.</param>
    /// <param name="property"><see cref="PropertyPath"/> that is wrong type.</param>
    public ChildrenPropertyIsNotEnumerableForType(Type type, PropertyPath property) : base($"Property at '{property.Path}' on '{type.FullName}' is not of enumerable type.")
    {
    }
}
