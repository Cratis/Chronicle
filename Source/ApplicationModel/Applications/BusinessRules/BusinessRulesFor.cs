// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Aksio.Cratis.Events.Projections;
using FluentValidation;
using FluentValidation.Results;

namespace Aksio.Cratis.Applications.BusinessRules;

/// <summary>
/// Represents a <see cref="AbstractValidator{T}"/> for business rules related to a command.
/// </summary>
/// <typeparam name="TSelf">The type of itself.</typeparam>
/// <typeparam name="TCommand">Type of command.</typeparam>
public abstract class BusinessRulesFor<TSelf, TCommand> : AbstractValidator<TCommand>
    where TSelf : BusinessRulesFor<TSelf, TCommand>
{
    readonly InlineValidator<TSelf> _selfValidator = new();

    /// <summary>
    /// Gets the unique identifier for the business rules.
    /// </summary>
    public abstract BusinessRulesId Identifier { get; }

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
        var rule = _selfValidator.RuleFor(expression);
        return new StateRuleBuilder<TSelf, TCommand, TProperty>(rule);
    }

    /// <inheritdoc/>
    public override ValidationResult Validate(ValidationContext<TCommand> context)
    {
        var result = base.Validate(context);

        var selfContext = context.CloneForChildValidator((TSelf)this, true);
        _selfValidator.Validate(selfContext);

        return result;
    }
}
