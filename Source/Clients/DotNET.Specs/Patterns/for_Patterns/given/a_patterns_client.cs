// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using ContractPatterns = Cratis.Chronicle.Contracts.Patterns.IPatterns;

namespace Cratis.Chronicle.Patterns.for_Patterns.given;

public class a_patterns_client : Specification
{
    protected const string EventStore = "some-store";
    protected const string Namespace = "some-namespace";

    protected Chronicle.Patterns.Patterns _client;
    protected IEventStore _eventStore;

    internal ContractPatterns _patterns;

    void Establish()
    {
        var connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        var services = Substitute.For<IServices>();
        _patterns = Substitute.For<ContractPatterns>();
        ((IChronicleServicesAccessor)connection).Services.Returns(services);
        services.Patterns.Returns(_patterns);

        _eventStore = Substitute.For<IEventStore>();
        _eventStore.Connection.Returns(connection);
        _eventStore.Name.Returns(new EventStoreName(EventStore));
        _eventStore.Namespace.Returns(new EventStoreNamespaceName(Namespace));

        _client = new(_eventStore);
    }
}
