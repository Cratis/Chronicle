// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Represents an implementation of <see cref="IConstraintValidationFactory"/>.
/// </summary>
/// <param name="storage">The <see cref="IStorage"/> to use.</param>
[Singleton]
public class ConstraintValidationFactory(IStorage storage) : IConstraintValidationFactory
{
    /// <inheritdoc/>
    public async Task<IConstraintValidation> Create(EventSequenceKey eventSequenceKey)
    {
        var eventStore = storage.GetEventStore(eventSequenceKey.EventStore);
        var namespaceStorage = eventStore.GetNamespace(eventSequenceKey.Namespace);
        var uniqueConstraintsStorage = namespaceStorage.GetUniqueConstraintsStorage(eventSequenceKey.EventSequenceId);
        var uniqueEventTypeConstraintsStorage = namespaceStorage.GetUniqueEventTypesConstraints(eventSequenceKey.EventSequenceId);
        var closedStreamsStorage = namespaceStorage.GetClosedStreamsConstraints(eventSequenceKey.EventSequenceId);
        var definitions = await eventStore.Constraints.GetDefinitions();
        var closedStreamDimensions = await closedStreamsStorage.GetDimensionsInUse();

        // A constraint that does not apply to this event sequence gets no validator at all, which skips both its
        // validation and the index update that follows a successful append - so the sequence never claims a value
        // it could only release through removal events appended to that same sequence.
        var validators = definitions
            .Where(_ => _.AppliesTo(eventSequenceKey.EventSequenceId))
            .Select<IConstraintDefinition, IConstraintValidator>(_ => _ switch
            {
                UniqueConstraintDefinition unique => new UniqueConstraintValidator(unique, uniqueConstraintsStorage),
                UniqueEventTypeConstraintDefinition uniqueEventType => new UniqueEventTypeConstraintValidator(uniqueEventType, uniqueEventTypeConstraintsStorage),
                _ => throw new UnknownConstraintType(_.GetType())
            })
            .Append(new ClosedStreamConstraintValidator(closedStreamsStorage, closedStreamDimensions))
            .ToArray();

        return new ConstraintValidation(validators);
    }
}
