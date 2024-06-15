// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Properties;

/// <summary>
/// Exception that gets thrown when a children property is not enumerable for a specific type.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ChildrenPropertyIsNotEnumerable"/> class.
/// </remarks>
/// <param name="type">Type that is the root of the <see cref="PropertyPath"/>.</param>
/// <param name="property"><see cref="PropertyPath"/> that is wrong type.</param>
public class ChildrenPropertyIsNotEnumerableForType(Type type, PropertyPath property) : Exception($"Property at '{property.Path}' on '{type.FullName}' is not of enumerable type.")
{
}
