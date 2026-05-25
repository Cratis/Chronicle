// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Chronicle.Concepts.Captures;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Represents an implementation of <see cref="IAppendBuilder{TEvent}"/>.
/// </summary>
/// <typeparam name="TEvent">The type of event to append.</typeparam>
public class AppendBuilder<TEvent> : IAppendBuilder<TEvent>
    where TEvent : class
{
    readonly Dictionary<string, string> _fieldAssignments = [];
    WhenClause? _when;

    /// <inheritdoc/>
    public IAppendBuilder<TEvent> WhenPropertyChanges(string property)
    {
        _when = new(WhenClauseType.PropertyChange, [property]);

        return this;
    }

    /// <inheritdoc/>
    public IAppendBuilder<TEvent> WhenAnyOf(params string[] properties)
    {
        _when = new(WhenClauseType.LogicalOr, properties);

        return this;
    }

    /// <inheritdoc/>
    public IAppendBuilder<TEvent> WhenAllOf(params string[] properties)
    {
        _when = new(WhenClauseType.LogicalAnd, properties);

        return this;
    }

    /// <inheritdoc/>
    public IAppendBuilder<TEvent> WhenTransition(string property, string from, string to)
    {
        _when = new(WhenClauseType.ValueTransition, [property], from, to);

        return this;
    }

    /// <inheritdoc/>
    public IAppendBuilder<TEvent> WhenAdded()
    {
        _when = new(WhenClauseType.Added, []);

        return this;
    }

    /// <inheritdoc/>
    public IAppendBuilder<TEvent> WhenRemoved()
    {
        _when = new(WhenClauseType.Removed, []);

        return this;
    }

    /// <inheritdoc/>
    public IAppendBuilder<TEvent> WhenExpression(string expression)
    {
        _when = new(WhenClauseType.Expression, [], Expression: expression);

        return this;
    }

    /// <inheritdoc/>
    public IAppendBuilder<TEvent> Set(Expression<Func<TEvent, object>> targetProperty, string sourceExpression)
    {
        var propertyPath = targetProperty.GetPropertyPath();
        if (!propertyPath.IsSet)
        {
            throw new TargetPropertyMustBeAProperty(typeof(TEvent));
        }

        _fieldAssignments[propertyPath.Path] = sourceExpression;

        return this;
    }

    /// <summary>
    /// Builds the <see cref="AppendDefinition"/>.
    /// </summary>
    /// <returns>A new <see cref="AppendDefinition"/>.</returns>
    public AppendDefinition Build()
    {
        ThrowIfConditionIsMissing();

        return new(typeof(TEvent).Name, _when!, new Dictionary<string, string>(_fieldAssignments));
    }

    void ThrowIfConditionIsMissing()
    {
        if (_when is null)
        {
            throw new MissingAppendCondition(typeof(TEvent));
        }
    }
}
