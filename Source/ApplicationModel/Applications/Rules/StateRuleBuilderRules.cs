// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using FluentValidation;

namespace Aksio.Cratis.Applications.Rules;

/// <summary>
/// Represents validation rules for <see cref="IStateRuleBuilder{TState, TCommand, TProperty}"/>.
/// </summary>
public static class StateRuleBuilderRules
{
    /// <summary>
    /// Defines a uniqueness rule for the current rule builder.
    /// </summary>
    /// <param name="ruleBuilder">The rule builder on which the validator should be defined.</param>
    /// <param name="expression">Expression representing the property on the command.</param>
    /// <typeparam name="TState">Type that holds the state.</typeparam>
    /// <typeparam name="TCommand">Type of command the rule is for.</typeparam>
    /// <typeparam name="TProperty">The type of the property being validated.</typeparam>
    /// <typeparam name="TCommandProperty">The type of the property on the command.</typeparam>
    /// <returns>Options for the rule.</returns>
    public static IRuleBuilderOptions<TState, TProperty> Unique<TState, TCommand, TProperty, TCommandProperty>(this IStateRuleBuilder<TState, TCommand, TProperty> ruleBuilder, Expression<Func<TCommand, TCommandProperty>> expression)
        where TState : RulesFor<TState, TCommand>
        where TProperty : IEnumerable<TCommandProperty>
    {
        var compiledExpression = expression.Compile();
        return ruleBuilder.SetValidator(new UniqueInstanceValidator<TState, TProperty>(_ => compiledExpression.Invoke((TCommand)_)!));
    }
}
