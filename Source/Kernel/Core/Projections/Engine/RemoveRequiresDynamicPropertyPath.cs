// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// The exception that is thrown when the remove expression targets a property path without a dynamic (dictionary key) segment.
/// </summary>
/// <param name="propertyPath">The <see cref="PropertyPath"/> the remove expression targets.</param>
public class RemoveRequiresDynamicPropertyPath(PropertyPath propertyPath)
    : Exception($"Property path '{propertyPath}' uses '{WellKnownExpressions.Remove}', which removes a dictionary key and therefore needs a dynamic key segment - for example '<dictionary>.$eventContent.<event property path>' or '<dictionary>.$eventContext.<event context path>'.");
