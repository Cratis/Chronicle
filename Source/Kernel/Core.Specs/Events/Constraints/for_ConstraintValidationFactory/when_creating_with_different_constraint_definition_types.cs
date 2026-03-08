// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintValidationFactory;

public class when_creating_with_different_constraint_definition_types : Specification
{
    EventSequenceKey _eventSequenceKey;
    IStorage _storage;
    IEventStoreStorage _eventStoreStorage;
    IEventStoreNamespaceStorage _namespaceStorage;
    IConstraintsStorage _constraintsStorage;
    IUniqueConstraintsStorage _uniqueConstraintsStorage;
    IUniqueEventTypesConstraintsStorage _uniqueEventTypesConstraintsStorage;
    IConstraintValidation _result;
    ConstraintValidationFactory _factory;

    void Establish()
    {
        _eventSequenceKey = new EventSequenceKey(EventSequenceId.Log, "SomeEventStore", "SomeNamespace");

        _storage = Substitute.For<IStorage>();
        _eventStoreStorage = Substitute.For<IEventStoreStorage>();
        _namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        _constraintsStorage = Substitute.For<IConstraintsStorage>();
        _uniqueConstraintsStorage = Substitute.For<IUniqueConstraintsStorage>();
        _uniqueEventTypesConstraintsStorage = Substitute.For<IUniqueEventTypesConstraintsStorage>();

        _storage.GetEventStore(_eventSequenceKey.EventStore).Returns(_eventStoreStorage);
        _eventStoreStorage.GetNamespace(_eventSequenceKey.Namespace).Returns(_namespaceStorage);
        _namespaceStorage.GetUniqueConstraintsStorage(_eventSequenceKey.EventSequenceId).Returns(_uniqueConstraintsStorage);
        _namespaceStorage.GetUniqueEventTypesConstraints(_eventSequenceKey.EventSequenceId).Returns(_uniqueEventTypesConstraintsStorage);
        _eventStoreStorage.Constraints.Returns(_constraintsStorage);
        _constraintsStorage.GetDefinitions().Returns([
            new UniqueConstraintDefinition("SomeUniqueConstraint", []),
            new UniqueEventTypeConstraintDefinition("SomeUniqueEventTypeConstraint", "SomeEventType")
        ]);

        _factory = new ConstraintValidationFactory(_storage);
    }

    async Task Because() => _result = await _factory.Create(_eventSequenceKey);

    [Fact] void should_return_a_constraint_validation_instance() => _result.ShouldNotBeNull();
}
