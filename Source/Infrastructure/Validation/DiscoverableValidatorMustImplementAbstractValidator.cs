// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Validation;

/// <summary>
/// Exception that gets thrown when a validator.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DiscoverableValidatorMustImplementAbstractValidator"/> class.
/// </remarks>
/// <param name="type">Validator type that is invalid.</param>
public class DiscoverableValidatorMustImplementAbstractValidator(Type type)
    : Exception($"Discoverable validator of type '{type.FullName}' does not derive from AbstractValidator<>, suggest using either BaseValidator<>, DiscoverableValidator<>, CommandValidator<>, QueryValidator<>, ConceptValidator<> or the AbstractValidator<> as base type.")
{
}
