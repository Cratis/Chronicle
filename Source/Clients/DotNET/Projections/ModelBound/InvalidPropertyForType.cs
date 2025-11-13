// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// The exception that is thrown when a property name does not exist on a type.
/// </summary>
/// <param name="type">The type the property was expected to be on.</param>
/// <param name="propertyName">The property name that was not found.</param>
public class InvalidPropertyForType(Type type, string propertyName) : Exception($"Property '{propertyName}' does not exist on type '{type.FullName}'")
{
}
