// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Represents an in-process implementation of <see cref="IConstraints"/> for test scenarios.
/// </summary>
/// <remarks>
/// <para>
/// Constraint definitions are discovered locally from the supplied <see cref="ICanProvideConstraints"/>
/// provider. Registration with the kernel is a no-op because the kernel grain already receives
/// the constraint definitions via <see cref="InMemoryConstraintsStorage"/> during setup.
/// </para>
/// </remarks>
/// <param name="constraintProvider">The <see cref="ICanProvideConstraints"/> that supplies constraint definitions.</param>
internal sealed class InProcessConstraints(ICanProvideConstraints constraintProvider) : IConstraints
{
    readonly List<IConstraintDefinition> _constraints = [];

    /// <inheritdoc/>
    public Task Discover()
    {
        _constraints.AddRange(constraintProvider.Provide());
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Register() => Task.CompletedTask;

    /// <inheritdoc/>
    public bool HasFor(ConstraintName constraintName) =>
        _constraints.Exists(_ => _.Name == constraintName);

    /// <inheritdoc/>
    public IConstraintDefinition GetFor(ConstraintName constraintName)
    {
        if (!HasFor(constraintName))
        {
            throw new UnknownConstraint(constraintName);
        }

        return _constraints.Single(_ => _.Name == constraintName);
    }

    /// <inheritdoc/>
    public ConstraintViolation ResolveMessageFor(ConstraintViolation violation)
    {
        if (!HasFor(violation.ConstraintName))
        {
            return violation;
        }

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
}
