// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;

namespace Aksio.Cratis.Applications.Rules;

/// <summary>
/// Defines a rule builder for state on a <see cref="RulesFor{T,TC}"/>.
/// </summary>
/// <typeparam name="TState">Type that holds the state.</typeparam>
/// <typeparam name="TCommand">Type of command the rule is for.</typeparam>
/// <typeparam name="TProperty">The type of the property being validated.</typeparam>
public interface IStateRuleBuilder<TState, TCommand, TProperty> : IRuleBuilderInitial<TState, TProperty>
    where TState : RulesFor<TState, TCommand>
{
}
