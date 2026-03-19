// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// The exception that is thrown when a property has been specified more than once in a projection definition.
/// </summary>
/// <param name="propertyPath">The <see cref="PropertyPath"/> that was defined more than once.</param>
public class DuplicatePropertyInProjection(PropertyPath propertyPath)
    : Exception($"Property '{propertyPath}' has already been defined in the projection definition. Each property can only be specified once.");
