// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Clients;
using Cratis.Chronicle.Contracts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Represents an implementation of <see cref="IConstraints"/>.
/// </summary>
/// <param name="eventStore"><see cref="IEventStore"/> for the constraints.</param>
/// <param name="constraintsProviders">Instances of <see cref="ICanProvideConstraints"/>.</param>
public class Constraints(
    IEventStore eventStore,
    IEnumerable<ICanProvideConstraints> constraintsProviders) : IConstraints
{
    readonly IChronicleServicesAccessor _servicesAccessor = (eventStore.Connection as IChronicleServicesAccessor)!;
    readonly List<IConstraintDefinition> _constraints = [];

    /// <inheritdoc/>
    public Task Discover()
    {
        _constraints.Clear();
        _constraints.AddRange(ClosesStreamDeclarations.Merge(constraintsProviders.SelectMany(provider => provider.Provide())));

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public IConstraintDefinition GetFor(ConstraintName constraintName)
    {
        ThrowIfUnknownConstraint(constraintName);

        // More than one provider can declare a name. Looking one up must not fail the resolution of a violation's message
        // for that, so it returns the first declaration rather than insisting there is only one.
        return _constraints.First(_ => _.Name == constraintName);
    }

    /// <inheritdoc/>
    public bool HasFor(ConstraintName constraintName) => _constraints.Exists(_ => _.Name == constraintName);

    /// <inheritdoc/>
    public Task Register()
    {
        if (_constraints.Exists(_ => _ is ClosesStreamConstraintDefinition) &&
            eventStore.Connection is IKernelCapabilities { CapabilitiesAreKnown: true } capabilities &&
            !capabilities.Capabilities.Contains(KernelCapabilities.ClosesStreamConstraints))
        {
            throw new ClosesStreamConstraintsNotSupported("The connected kernel does not support closing stream constraints. Upgrade the kernel before registering closing declarations.");
        }

        var request = new RegisterConstraintsRequest
        {
            EventStore = eventStore.Name,
            Constraints = _constraints.ConvertAll(_ => _.ToContract())
        };
        return _servicesAccessor.Services.Constraints.Register(request);
    }

    /// <inheritdoc/>
    public ConstraintViolation ResolveMessageFor(ConstraintViolation violation)
    {
        // Schema validation and other built-in constraints don't have user-defined constraint definitions
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

    void ThrowIfUnknownConstraint(ConstraintName constraintName)
    {
        if (!HasFor(constraintName)) throw new UnknownConstraint(constraintName);
    }
}
