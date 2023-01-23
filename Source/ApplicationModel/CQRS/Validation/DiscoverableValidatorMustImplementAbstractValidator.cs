// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Applications.Validation;

/// <summary>
/// Exception that gets thrown when a validator.
/// </summary>
public class DiscoverableValidatorMustImplementAbstractValidator : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DiscoverableValidatorMustImplementAbstractValidator"/> class.
    /// </summary>
    /// <param name="type">Validator type that is invalid.</param>
    public DiscoverableValidatorMustImplementAbstractValidator(Type type) : base($"Discoverable validator of type '{type.FullName}' does not derive from AbstractValidator<>, suggest using either BaseValidator<>, DiscoverableValidator<>, CommandValidator<>, QueryValidator<>, ConceptValidator<> or the AbstractValidator<> as base type.")
    {
    }
}
