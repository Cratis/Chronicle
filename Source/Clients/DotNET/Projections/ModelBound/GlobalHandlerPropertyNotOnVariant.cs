// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// The exception that is thrown when a <see cref="GlobalForAttribute{TIdentity}"/> handler maps a member that a
/// variant of the same identity does not have.
/// </summary>
/// <param name="globalHandlerType">The type declaring the shared handler.</param>
/// <param name="variantType">The variant the mapping cannot be applied to.</param>
/// <param name="propertyName">The member the shared handler maps.</param>
public class GlobalHandlerPropertyNotOnVariant(Type globalHandlerType, Type variantType, string propertyName)
    : Exception($"Global handler '{globalHandlerType.FullName}' maps '{propertyName}', which variant '{variantType.FullName}' does not have. Either add it to the variant or move the mapping onto the variants that have it.");
