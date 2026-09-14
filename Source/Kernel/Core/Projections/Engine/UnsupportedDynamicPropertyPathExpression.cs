// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// The exception that is thrown when a dynamic dictionary-key property path segment does not reference
/// a well-known expression that can be resolved against the current event.
/// </summary>
/// <param name="propertyPath">The <see cref="PropertyPath"/> containing the unsupported dynamic segment.</param>
public class UnsupportedDynamicPropertyPathExpression(PropertyPath propertyPath)
    : Exception($"Property path '{propertyPath}' contains a dynamic segment that could not be resolved. Supported dynamic segments are of the form '.$eventContext.<property path>'.");
