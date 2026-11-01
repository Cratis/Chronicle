// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Chronicle.Concepts.Registrations;
using FluentValidation;

namespace Cratis.Chronicle.EventStores;

/// <summary>
/// Represents the validator for <see cref="DeploymentGeneration"/>, which must fit the signed 64-bit storage it is kept in.
/// </summary>
public class DeploymentGenerationValidator : ConceptValidator<DeploymentGeneration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeploymentGenerationValidator"/> class.
    /// </summary>
    public DeploymentGenerationValidator() => RuleFor(_ => _.Value).LessThanOrEqualTo((ulong)long.MaxValue);
}
