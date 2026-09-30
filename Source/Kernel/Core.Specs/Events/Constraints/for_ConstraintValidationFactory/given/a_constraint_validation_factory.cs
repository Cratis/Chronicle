// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintValidationFactory.given;

public class a_constraint_validation_factory : Specification
{
    protected static readonly UniqueConstraintDefinition _unique = new("unique-name", [new UniqueConstraintEventDefinition("name-claimed", ["Name"])]);
    protected static readonly UniqueEventTypeConstraintDefinition _uniqueEventType = new("once-only", [(EventTypeId)"name-claimed"]);

    protected IStorage _storage;
    protected IEventStoreStorage _eventStoreStorage;
    protected IEventStoreNamespaceStorage _namespaceStorage;
    protected IConstraintsStorage _constraintsStorage;
    protected List<IConstraintDefinition> _definitions;
    protected ConstraintValidationFactory _factory;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        _eventStoreStorage = Substitute.For<IEventStoreStorage>();
        _namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        _constraintsStorage = Substitute.For<IConstraintsStorage>();
        _definitions = [];

        _storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(_eventStoreStorage);
        _eventStoreStorage.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(_namespaceStorage);
        _namespaceStorage.GetUniqueConstraintsStorage(Arg.Any<EventSequenceId>()).Returns(Substitute.For<IUniqueConstraintsStorage>());
        _namespaceStorage.GetUniqueEventTypesConstraints(Arg.Any<EventSequenceId>()).Returns(Substitute.For<IUniqueEventTypesConstraintsStorage>());
        _namespaceStorage.GetClosedStreamsConstraints(Arg.Any<EventSequenceId>()).Returns(Substitute.For<IClosedStreamsConstraintStorage>());
        _eventStoreStorage.Constraints.Returns(_constraintsStorage);
        _constraintsStorage.GetDefinitions().Returns(_ => _definitions);

        _factory = new ConstraintValidationFactory(_storage);
    }

    protected static EventSequenceKey KeyFor(EventSequenceId eventSequenceId) => new(eventSequenceId, "some-event-store", "some-namespace");

    protected static IEnumerable<IConstraintDefinition> DefinitionsValidatedBy(IConstraintValidation validation) =>
        validation
            .Establish("some-event-source", "name-claimed", new System.Dynamic.ExpandoObject())
            .Validators
            .Select(_ => _.Definition)
            .Where(_ => _ is not ClosedStreamConstraintDefinition);
}
