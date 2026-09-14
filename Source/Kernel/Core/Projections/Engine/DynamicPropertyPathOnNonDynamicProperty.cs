// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// The exception that is thrown when a dynamic property path segment (.$expression) is used on a property that is not marked as dynamic.
/// </summary>
/// <param name="propertyPath">The property path containing the invalid dynamic segment.</param>
/// <param name="basePropertyName">The name of the base property that is not dynamic.</param>
public class DynamicPropertyPathOnNonDynamicProperty(PropertyPath propertyPath, string basePropertyName)
    : Exception($"Property path '{propertyPath}' contains a dynamic segment (.$...) but the base property '{basePropertyName}' is not a dynamic (dictionary) property. Dynamic property segments can only be used on properties that are dictionaries or marked as dynamic.");
