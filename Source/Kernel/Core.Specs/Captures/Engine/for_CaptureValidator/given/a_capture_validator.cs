// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Captures;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.ExternalServices;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.ExternalServices;

namespace Cratis.Chronicle.Captures.Engine.for_CaptureValidator.given;

public class a_capture_validator : Specification
{
    protected const string ExternalServiceName = "CustomersApi";
    protected const string EventTypeName = "CustomerChanged";

    protected CaptureValidator _validator;
    protected IStorage _storage;
    protected IEventStoreStorage _eventStoreStorage;
    protected IExternalServiceDefinitionsStorage _externalServices;
    protected IEventTypesStorage _eventTypes;
    protected EventStoreName _eventStore;

    void Establish()
    {
        _eventStore = "some-event-store";
        _storage = Substitute.For<IStorage>();
        _eventStoreStorage = Substitute.For<IEventStoreStorage>();
        _externalServices = Substitute.For<IExternalServiceDefinitionsStorage>();
        _eventTypes = Substitute.For<IEventTypesStorage>();
        _storage.GetEventStore(_eventStore).Returns(_eventStoreStorage);
        _eventStoreStorage.ExternalServices.Returns(_externalServices);
        _eventStoreStorage.EventTypes.Returns(_eventTypes);

        _externalServices.GetAll().Returns(
        [
            new ExternalServiceDefinition(
                "customers-api",
                ExternalServiceName,
                new ExternalServiceEndpoint(
                    ExternalServiceEndpointType.Http,
                    new HttpEndpointConfiguration("https://example.com", ExternalServiceAuthorization.None, new Dictionary<string, string>())))
        ]);
        _eventTypes.HasFor(new EventTypeId(EventTypeName)).Returns(true);

        _validator = new(_storage);
    }

    protected static CaptureDefinition CreateDefinition(
        SourceDefinition? source = null,
        IReadOnlyList<AppendDefinition>? appends = null,
        MapDefinition? map = null,
        IReadOnlyList<NestedDefinition>? nested = null,
        IReadOnlyList<ChildrenDefinition>? children = null) =>
        new(
            CaptureId.NotSet,
            "Customers",
            source ?? new SourceDefinition(SourceType.Api, Api: ExternalServiceName, Poll: "5m"),
            "customerId",
            map,
            appends ?? [new AppendDefinition(EventTypeName, new WhenClause(WhenClauseType.Added, []), new Dictionary<string, string>())],
            nested ?? [],
            children ?? []);
}
