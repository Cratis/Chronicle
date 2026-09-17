// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// The exception that is thrown when a type decorated with <see cref="VariantOfAttribute{TIdentity}"/> does not
/// declare a <see cref="Keys.KeyAttribute"/> on any property or primary constructor parameter.
/// </summary>
/// <param name="modelType">The variant type that is missing the <see cref="Keys.KeyAttribute"/>.</param>
public class VariantMustDeclareKeyProperty(Type modelType)
    : Exception($"Variant '{modelType.FullName}' must declare a [Key] property so its own identity can be correlated when a shared handler updates it.");
