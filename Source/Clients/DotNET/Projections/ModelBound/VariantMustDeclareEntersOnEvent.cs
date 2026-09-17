// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// The exception that is thrown when a type decorated with <see cref="VariantOfAttribute{TIdentity}"/> does not
/// declare at least one <see cref="EntersOnAttribute{TEvent}"/>.
/// </summary>
/// <param name="modelType">The variant type that is missing the <see cref="EntersOnAttribute{TEvent}"/>.</param>
public class VariantMustDeclareEntersOnEvent(Type modelType)
    : Exception($"Variant '{modelType.FullName}' must declare at least one [EntersOn<TEvent>] to specify which event activates it.");
