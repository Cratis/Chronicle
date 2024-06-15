// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Properties;

/// <summary>
/// Exception that gets thrown when a children property is not enumerable.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ChildrenPropertyIsNotEnumerable"/> class.
/// </remarks>
/// <param name="property"><see cref="PropertyPath"/> that is wrong type.</param>
public class ChildrenPropertyIsNotEnumerable(PropertyPath property) : Exception($"Property at '{property.Path}' is not of enumerable type.")
{
}
