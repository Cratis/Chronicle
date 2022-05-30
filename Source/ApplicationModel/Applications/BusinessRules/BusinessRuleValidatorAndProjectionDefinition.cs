// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Projections.Definitions;
using FluentValidation;

namespace Aksio.Cratis.Applications.BusinessRules;

/// <summary>
/// Represents a combination of <see cref="IValidator"/> and <see cref="ProjectionDefinition"/> that makes up a <see cref="BusinessRulesFor{TSelf, TCommand}"/>.
/// </summary>
/// <param name="Identifier">Identifier of the business rules.</param>
/// <param name="Validator">Validator to use.</param>
/// <param name="ProjectionDefinition">Projection definition.</param>
public record BusinessRuleValidatorAndProjectionDefinition(BusinessRulesId Identifier, IValidator Validator, ProjectionDefinition ProjectionDefinition);
