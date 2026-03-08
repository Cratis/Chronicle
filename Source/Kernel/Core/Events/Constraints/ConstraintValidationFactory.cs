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
        var definitions = await eventStore.Constraints.GetDefinitions();
        var validators = definitions.Select<IConstraintDefinition, IConstraintValidator>(_ => _ switch
        {
            UniqueConstraintDefinition unique => new UniqueConstraintValidator(unique, uniqueConstraintsStorage),
            UniqueEventTypeConstraintDefinition uniqueEventType => new UniqueEventTypeConstraintValidator(uniqueEventType, uniqueEventTypeConstraintsStorage),
            _ => throw new UnknownConstraintType(_.GetType())
        }).ToArray();

        return new ConstraintValidation(validators);
    }
}
