// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Observation.Webhooks;
using Cratis.Chronicle.Contracts.Primitives;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Events;
using Microsoft.Extensions.Logging;

using IWebhooks = Cratis.Chronicle.Contracts.Observation.Webhooks.IWebhooks;

namespace Cratis.Chronicle.Specs.Webhooks.for_Webhooks.given;

public class signing_key_query_responses : Specification
{
    protected Chronicle.Webhooks.Webhooks _webhooks;
    protected IWebhooks _service;
    protected QueryResult<IEnumerable<WebhookSigningPublicKeyResponse>> _result;
    protected GetWebhookSigningKeysRequest _request;

    void Establish()
    {
        var connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        var servicesAccessor = (IChronicleServicesAccessor)connection;
        var services = Substitute.For<IServices>();
        _service = Substitute.For<IWebhooks>();
        servicesAccessor.Services.Returns(services);
        services.Webhooks.Returns(_service);
        var eventStore = Substitute.For<IEventStore>();
        eventStore.Name.Returns(new EventStoreName("some-event-store"));
        eventStore.Connection.Returns(connection);
        _result = QueryResult<IEnumerable<WebhookSigningPublicKeyResponse>>.Success(Guid.Empty, []);
        _service.GetWebhookSigningKeys(Arg.Do<GetWebhookSigningKeysRequest>(request => _request = request)).Returns(_ => _result);
        _webhooks = new(Substitute.For<IEventTypes>(), eventStore, Substitute.For<ILogger<Chronicle.Webhooks.Webhooks>>());
    }

    protected static WebhookSigningPublicKeyResponse Key(string keyId, WebhookSigningKeyStatus status, string overlapEndsAt = "") => new()
    {
        KeyId = keyId,
        Algorithm = WebhookSigningAlgorithm.Ed25519,
        PublicKey = "whpk_" + keyId,
        Status = status,
        CreatedAt = new SerializableDateTimeOffset { Value = "2026-01-02T03:04:05.0000000+00:00" },
        OverlapEndsAt = new SerializableDateTimeOffset { Value = overlapEndsAt }
    };
}
