// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;

using Cratis.Chronicle.Storage.Events.Constraints;
using ClientConstraints = Cratis.Chronicle.Events.Constraints;
using KernelConstraints = KernelConcepts::Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Represents an in-memory implementation of <see cref="IConstraintsStorage"/> for testing.
/// </summary>
/// <remarks>
/// Converts client-side <see cref="ClientConstraints.IConstraintDefinition"/> objects (discovered via
/// <see cref="ClientConstraints.ICanProvideConstraints"/>) into kernel-type
/// <see cref="KernelConstraints::IConstraintDefinition"/> objects at construction time.
/// </remarks>
/// <param name="clientConstraintProvider">The <see cref="ClientConstraints.ICanProvideConstraints"/> to load definitions from.</param>
internal sealed class InMemoryConstraintsStorage(ClientConstraints.ICanProvideConstraints clientConstraintProvider) : IConstraintsStorage
{
    IEnumerable<KernelConstraints::IConstraintDefinition>? _kernelDefinitions;

    /// <inheritdoc/>
    public Task<IEnumerable<KernelConstraints::IConstraintDefinition>> GetDefinitions()
    {
        _kernelDefinitions ??= clientConstraintProvider
            .Provide()
            .Select(ToKernel)
            .OfType<KernelConstraints::IConstraintDefinition>()
            .ToList();

        return Task.FromResult(_kernelDefinitions);
    }

    /// <inheritdoc/>
    public Task SaveDefinition(KernelConstraints::IConstraintDefinition definition) => Task.CompletedTask;

    static KernelConstraints::IConstraintDefinition? ToKernel(ClientConstraints.IConstraintDefinition client) =>
        client switch
        {
            ClientConstraints.UniqueConstraintDefinition unique => new KernelConstraints::UniqueConstraintDefinition(
                (KernelConstraints::ConstraintName)unique.Name.Value,
                unique.EventsWithProperties.Select(e => new KernelConstraints::UniqueConstraintEventDefinition(
                    (KernelConcepts::Cratis.Chronicle.Concepts.Events.EventTypeId)e.EventTypeId.Value,
                    e.Properties)),
                unique.RemovedWith is not null
                    ? (KernelConcepts::Cratis.Chronicle.Concepts.Events.EventTypeId)unique.RemovedWith.Value.Value
                    : null,
                unique.IgnoreCasing),

            ClientConstraints.UniqueEventTypeConstraintDefinition uniqueType => new KernelConstraints::UniqueEventTypeConstraintDefinition(
                (KernelConstraints::ConstraintName)uniqueType.Name.Value,
                (KernelConcepts::Cratis.Chronicle.Concepts.Events.EventTypeId)uniqueType.EventTypeId.Value),

            _ => null
        };
}
