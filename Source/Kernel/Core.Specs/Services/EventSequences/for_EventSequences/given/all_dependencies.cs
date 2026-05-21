// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using KernelEventSequences = Cratis.Chronicle.Services.EventSequences.EventSequences;

namespace Cratis.Chronicle.Services.EventSequences.for_EventSequences.given;

public class all_dependencies : Specification
{
    protected IStorage _storage;
    protected IEventStoreStorage _eventStoreStorage;
    protected IEventStoreNamespaceStorage _namespaceStorage;
    protected IEventSequenceStorage _eventSequenceStorage;
    protected IGrainFactory _grainFactory;
    protected IJsonComplianceManager _complianceManager;
    protected IExpandoObjectConverter _expandoObjectConverter;
    protected Contracts.EventSequences.IEventSequences _service;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        _eventStoreStorage = Substitute.For<IEventStoreStorage>();
        _namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        _eventSequenceStorage = Substitute.For<IEventSequenceStorage>();
        _grainFactory = Substitute.For<IGrainFactory>();
        _complianceManager = Substitute.For<IJsonComplianceManager>();
        _expandoObjectConverter = Substitute.For<IExpandoObjectConverter>();

        _storage.GetEventStore(Arg.Any<Concepts.EventStoreName>()).Returns(_eventStoreStorage);
        _eventStoreStorage.GetNamespace(Arg.Any<Concepts.EventStoreNamespaceName>()).Returns(_namespaceStorage);
        _namespaceStorage.GetEventSequence(Arg.Any<Concepts.EventSequences.EventSequenceId>()).Returns(_eventSequenceStorage);

        _service = new KernelEventSequences(
            _grainFactory,
            _storage,
            _complianceManager,
            _expandoObjectConverter,
            new JsonSerializerOptions());
    }
}
