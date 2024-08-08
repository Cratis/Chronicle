// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events.Constraints;
using Cratis.Collections;
using Cratis.Types;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Represents an implementation of <see cref="IConstraints"/>.
/// </summary>
/// <param name="eventStore"><see cref="IEventStore"/> for the constraints.</param>
/// <param name="constraintsProviders">Instances of <see cref="ICanProvideConstraints"/>.</param>
public class Constraints(
    IEventStore eventStore,
    IInstancesOf<ICanProvideConstraints> constraintsProviders) : IConstraints
{
    readonly List<IConstraintDefinition> _constraints = [];

    /// <inheritdoc/>
    public Task Discover()
    {
        constraintsProviders.ForEach(provider => _constraints.AddRange(provider.Provide()));

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public IConstraintDefinition GetFor(ConstraintName constraintName)
    {
        ThrowIfUnknownConstraint(constraintName);
        return _constraints.Single(_ => _.Name == constraintName);
    }

    /// <inheritdoc/>
    public bool HasFor(ConstraintName constraintName) => _constraints.Exists(_ => _.Name == constraintName);

    /// <inheritdoc/>
    public Task Register()
    {
        var request = new RegisterConstraintsRequest
        {
            EventStoreName = eventStore.Name,
            Constraints = _constraints.ConvertAll(_ => _.ToContract())
        };
        return eventStore.Connection.Services.Constraints.Register(request);
    }

    /// <inheritdoc/>
    public ConstraintViolation ResolveMessageFor(ConstraintViolation violation)
    {
        var constraint = GetFor(violation.ConstraintName);
        var message = constraint.MessageCallback(violation);

        foreach (var (detailKey, detailValue) in violation.Details)
        {
            message = message.Value.Replace($"{{{detailKey}}}", detailValue);
        }

        if (string.IsNullOrEmpty(message.Value))
        {
            return violation;
        }

        return violation with { Message = message };
    }

    void ThrowIfUnknownConstraint(ConstraintName constraintName)
    {
        if (!HasFor(constraintName)) throw new UnknownConstraint(constraintName);
    }
}
