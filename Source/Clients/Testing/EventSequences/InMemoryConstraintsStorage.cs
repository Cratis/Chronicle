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
        _kernelDefinitions ??= ClientConstraints.ClosesStreamDeclarations.Merge(clientConstraintProvider.Provide())
            .Select(ToKernel)
            .OfType<KernelConstraints::IConstraintDefinition>()
            .ToList();

        return Task.FromResult(_kernelDefinitions);
    }

    /// <inheritdoc/>
    public Task SaveDefinition(KernelConstraints::IConstraintDefinition definition) => Task.CompletedTask;

    static KernelConstraints::IConstraintDefinition? ToKernel(ClientConstraints.IConstraintDefinition client)
    {
        if (client is ClientConstraints.UniqueConstraintDefinition unique)
        {
            var eventsWithProperties = unique.EventsWithProperties.Select(e =>
                new KernelConstraints::UniqueConstraintEventDefinition(
                    (KernelConcepts::Cratis.Chronicle.Concepts.Events.EventTypeId)e.EventTypeId.Value,
                    e.Properties));

            return new KernelConstraints::UniqueConstraintDefinition(
                (KernelConstraints::ConstraintName)unique.Name.Value,
                eventsWithProperties,
                ToKernelEventTypeIds(unique.RemovedWith),
                unique.IgnoreCasing,
                ToKernelScope(unique.Scope))
            {
                EventSequences = ToKernelEventSequenceIds(unique.EventSequences),
                Mode = (KernelConstraints::UniqueConstraintMode)unique.Mode,
                RemovalEventDefinitions = unique.RemovalEventDefinitions.Select(e =>
                    new KernelConstraints::UniqueConstraintEventDefinition(
                        (KernelConcepts::Cratis.Chronicle.Concepts.Events.EventTypeId)e.EventTypeId.Value,
                        e.Properties)).ToArray()
            };
        }

        if (client is ClientConstraints.UniqueEventTypeConstraintDefinition uniqueType)
        {
            return new KernelConstraints::UniqueEventTypeConstraintDefinition(
                (KernelConstraints::ConstraintName)uniqueType.Name.Value,
                ToKernelEventTypeIds(uniqueType.EventTypeIds),
                ToKernelEventTypeIds(uniqueType.RemovedWith),
                ToKernelScope(uniqueType.Scope))
            {
                EventSequences = ToKernelEventSequenceIds(uniqueType.EventSequences)
            };
        }

        if (client is ClientConstraints.ClosesStreamConstraintDefinition closing)
        {
            return new KernelConstraints::ClosesStreamConstraintDefinition(
                closing.Name.Value,
                ToKernelEventTypeIds(closing.EventTypeIds),
                (KernelConstraints::ClosedStreamDimensions)closing.Dimensions,
                ToKernelEventTypeIds(closing.ReopenedBy),
                closing.EventStreamIdFrom)
            {
                EventSequences = ToKernelEventSequenceIds(closing.EventSequences)
            };
        }

        return null;
    }

    static KernelConstraints::ConstraintScope? ToKernelScope(ClientConstraints.ConstraintScope? scope)
    {
        if (scope?.HasScope != true)
        {
            return null;
        }

        return new KernelConstraints::ConstraintScope(
            scope.EventSourceType is not null ? (KernelConcepts::Cratis.Chronicle.Concepts.Events.EventSourceType)scope.EventSourceType.Value : null,
            scope.EventStreamType is not null ? (KernelConcepts::Cratis.Chronicle.Concepts.Events.EventStreamType)scope.EventStreamType.Value : null,
            scope.EventStreamId is not null ? (KernelConcepts::Cratis.Chronicle.Concepts.Events.EventStreamId)scope.EventStreamId.Value : null);
    }

    static KernelConcepts::Cratis.Chronicle.Concepts.Events.EventTypeId[] ToKernelEventTypeIds(IEnumerable<global::Cratis.Chronicle.Events.EventTypeId> eventTypeIds) =>
        [.. eventTypeIds.Select(_ => (KernelConcepts::Cratis.Chronicle.Concepts.Events.EventTypeId)_.Value)];

    static KernelConcepts::Cratis.Chronicle.Concepts.EventSequences.EventSequenceId[] ToKernelEventSequenceIds(IEnumerable<global::Cratis.Chronicle.EventSequences.EventSequenceId> eventSequenceIds) =>
        [.. eventSequenceIds.Select(_ => (KernelConcepts::Cratis.Chronicle.Concepts.EventSequences.EventSequenceId)_.Value)];
}
