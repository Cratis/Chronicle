// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;
using FluentValidation.Validators;

namespace Cratis.Chronicle.Rules;

/// <summary>
/// Represents an implementation of <see cref="IStateRuleBuilder{TState, TCommand, TProperty}"/>.
/// </summary>
/// <typeparam name="TState">Type that holds the state.</typeparam>
/// <typeparam name="TCommand">Type of command the rule is for.</typeparam>
/// <typeparam name="TProperty">The type of the property being validated.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="StateRuleBuilder{TState, TCommand, TProperty}"/> class.
/// </remarks>
/// <param name="innerBuilder">The inner builder to use.</param>
public class StateRuleBuilder<TState, TCommand, TProperty>(IRuleBuilderInitial<TState, TProperty> innerBuilder) : IStateRuleBuilder<TState, TCommand, TProperty>
    where TState : RulesFor<TState, TCommand>
{
    /// <inheritdoc/>
    public IRuleBuilderOptions<TState, TProperty> SetAsyncValidator(IAsyncPropertyValidator<TState, TProperty> validator) => innerBuilder.SetAsyncValidator(validator);

    /// <inheritdoc/>
    public IRuleBuilderOptions<TState, TProperty> SetValidator(IPropertyValidator<TState, TProperty> validator) => innerBuilder.SetValidator(validator);

    /// <inheritdoc/>
    public IRuleBuilderOptions<TState, TProperty> SetValidator(IValidator<TProperty> validator, params string[] ruleSets) => innerBuilder.SetValidator(validator, ruleSets);

    /// <inheritdoc/>
    public IRuleBuilderOptions<TState, TProperty> SetValidator<TValidator>(Func<TState, TValidator> validatorProvider, params string[] ruleSets)
        where TValidator : IValidator<TProperty>
        => innerBuilder.SetValidator(validatorProvider, ruleSets);

    /// <inheritdoc/>
    public IRuleBuilderOptions<TState, TProperty> SetValidator<TValidator>(Func<TState, TProperty, TValidator> validatorProvider, params string[] ruleSets)
        where TValidator : IValidator<TProperty>
        => innerBuilder.SetValidator(validatorProvider, ruleSets);
}
