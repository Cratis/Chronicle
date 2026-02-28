// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Properties;
using Catch = Cratis.Monads.Catch;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookMediator.when_calling_on_next;

public class with_valid_webhook_target : given.a_webhook_mediator
{
    WebhookTarget _target;
    Key _partition;
    IEnumerable<AppendedEvent> _events;
    Catch _result;
    FakeHttpMessageHandler _fakeHandler;
    HttpClient _httpClient;

    void Establish()
    {
        _partition = new Key("test-partition", ArrayIndexers.NoIndexers);
        _target = new WebhookTarget(
            new WebhookTargetUrl("https://example.com/webhook"),
            WebhookAuthorization.None,
            new Dictionary<string, string>());

        var content = new ExpandoObject();
        _events = [new AppendedEvent(
            EventContext.From(
                EventStoreName.NotSet,
                EventStoreNamespaceName.NotSet,
                "EventType1",
                EventSourceType.Default,
                new EventSourceId("source-1"),
                EventStreamType.All,
                EventStreamId.Default,
                0,
                CorrelationId.NotSet,
                tags: null,
                DateTimeOffset.UtcNow),
            content)];

        _fakeHandler = new FakeHttpMessageHandler(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        _httpClient = new HttpClient(_fakeHandler)
        {
            BaseAddress = new Uri("https://example.com/webhook")
        };
        _httpClientFactory.Create(_target).Returns(_httpClient);
    }

    async Task Because() => _result = await _mediator.OnNext(_target, _partition, _events);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();

    [Fact] void should_create_http_client() => _httpClientFactory.Received(1).Create(_target);

    [Fact] void should_post_events_to_webhook() => _fakeHandler.RequestsSent.ShouldEqual(1);
}
