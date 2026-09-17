// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections;

/// <summary>
/// The exception that is thrown when a model-bound variant does not declare a <see cref="Keys.KeyAttribute"/> on
/// any property or primary constructor parameter.
/// </summary>
/// <remarks>
/// The key is what correlates an update-only handler back to an already-active instance of the variant, so a
/// variant without one has no way to be updated after it is created.
/// </remarks>
/// <param name="modelType">The variant type that is missing the <see cref="Keys.KeyAttribute"/>.</param>
public class VariantMustDeclareKeyProperty(Type modelType)
    : Exception($"Variant '{modelType.FullName}' must declare a [Key] property so its own identity can be correlated when a shared handler updates it.");
