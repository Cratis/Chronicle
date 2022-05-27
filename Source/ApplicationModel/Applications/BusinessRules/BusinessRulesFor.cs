// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Aksio.Cratis.Events.Projections;
using FluentValidation;

namespace Aksio.Cratis.Applications.BusinessRules;

/// <summary>
/// Represents a <see cref="AbstractValidator{T}"/> for business rules related to a command.
/// </summary>
/// <typeparam name="TSelf">The type of itself.</typeparam>
/// <typeparam name="TCommand">Type of command.</typeparam>
public class BusinessRulesFor<TSelf, TCommand> : AbstractValidator<TCommand>
    where TSelf : BusinessRulesFor<TSelf, TCommand>
{
    /// <summary>
    /// Define state that will be used by rules.
    /// </summary>
    /// <param name="builder"><see cref="IProjectionBuilderFor{T}"/> for building the state.</param>
    public virtual void DefineState(IProjectionBuilderFor<TSelf> builder)
    {
    }

    /// <summary>
    /// Defines a validation rule for a specific property on the state.
    /// </summary>
    /// <param name="expression"> The expression representing the property to validate.</param>
    /// <typeparam name="TProperty">The type of property being validated.</typeparam>
    /// <returns>IRuleBuilder instance on which validators can be defined.</returns>
    public IStateRuleBuilder<TSelf, TCommand, TProperty> RuleForState<TProperty>(Expression<Func<TSelf, TProperty>> expression)
    {
        #pragma warning disable RCS1140
        throw new NotImplementedException();
    }
}
