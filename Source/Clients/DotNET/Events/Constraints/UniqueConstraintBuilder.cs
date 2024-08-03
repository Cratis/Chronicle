// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Represents an implementation of <see cref="IUniqueConstraintBuilder"/>.
/// </summary>
/// <param name="eventTypes">Event types for the builder.</param>
/// <param name="owner">Optional owner of the constraint.</param>
public class UniqueConstraintBuilder(IEventTypes eventTypes, Type? owner = default) : IUniqueConstraintBuilder
{
    /// <inheritdoc/>
    public IUniqueConstraintBuilder On<TEventType>(Expression<Func<TEventType, object>> property) => throw new NotImplementedException();

    /// <inheritdoc/>
    public IUniqueConstraintBuilder On(EventType eventType, string property) => throw new NotImplementedException();

    /// <inheritdoc/>
    public IUniqueConstraintBuilder WithMessage(string message) => throw new NotImplementedException();

    /// <inheritdoc/>
    public IUniqueConstraintBuilder WithMessage(Func<object, ConstraintViolationMessage> messageProvider) => throw new NotImplementedException();

    /// <inheritdoc/>
    public IUniqueConstraintBuilder WithName(ConstraintName name) => throw new NotImplementedException();

    /// <inheritdoc/>
    public IConstraintDefinition Build()
    {
        return new UniqueConstraintDefinition(string.Empty, (et) => string.Empty, []);
    }
}
