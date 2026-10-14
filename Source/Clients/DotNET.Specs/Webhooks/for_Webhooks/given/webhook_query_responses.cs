// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Observation.Webhooks;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Events;
using Microsoft.Extensions.Logging;

using IWebhooks = Cratis.Chronicle.Contracts.Observation.Webhooks.IWebhooks;

namespace Cratis.Chronicle.Specs.Webhooks.for_Webhooks.given;

public class webhook_query_responses : Specification
{
    protected Chronicle.Webhooks.Webhooks _webhooks;
    protected WebhookDetailsResponse _response;
    protected Chronicle.Webhooks.WebhookDefinition _definition;

    void Establish()
    {
        var connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        var servicesAccessor = (IChronicleServicesAccessor)connection;
        var services = Substitute.For<IServices>();
        var webhooks = Substitute.For<IWebhooks>();
        servicesAccessor.Services.Returns(services);
        services.Webhooks.Returns(webhooks);
        var eventStore = Substitute.For<IEventStore>();
        eventStore.Name.Returns(new EventStoreName("store"));
        eventStore.Connection.Returns(connection);
        _response = new()
        {
            Id = "webhook",
            Identifier = "webhook",
            Url = "https://example.com/webhook",
            EventSequenceId = "log",
            EventTypes = [new() { Id = "person-registered", Generation = 2 }],
            Headers = new Dictionary<string, string>(),
            IsReplayable = true,
            IsActive = true
        };
        webhooks.GetWebhooks(Arg.Any<GetWebhooksRequest>()).Returns(_ => QueryResult<IEnumerable<WebhookDetailsResponse>>.Success(Guid.Empty, [_response]));
        _webhooks = new(Substitute.For<IEventTypes>(), eventStore, Substitute.For<ILogger<Chronicle.Webhooks.Webhooks>>());
    }
}
