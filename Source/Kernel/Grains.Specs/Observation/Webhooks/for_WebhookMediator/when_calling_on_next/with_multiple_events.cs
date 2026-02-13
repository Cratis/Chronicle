// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Properties;
using Catch = Cratis.Monads.Catch;

namespace Cratis.Chronicle.Grains.Observation.Webhooks.for_WebhookMediator.when_calling_on_next;

public class with_multiple_events : given.a_webhook_mediator
{
    WebhookTarget _target;
    Key _partition;
    IEnumerable<AppendedEvent> _events;
    Catch _result;
    HttpClient _httpClient;
    FakeHttpMessageHandler _fakeHandler;

    void Establish()
    {
        _partition = new Key("test-partition", ArrayIndexers.NoIndexers);
        _target = new WebhookTarget(
            new WebhookTargetUrl("https://example.com/webhook"),
            WebhookAuthorization.None,
            new Dictionary<string, string>());

        var content = new ExpandoObject();
        _events = Enumerable.Range(0, 5)
            .Select(i => new AppendedEvent(
                EventContext.From(
                    EventStoreName.NotSet,
                    EventStoreNamespaceName.NotSet,
                    $"EventType{i}",
                    EventSourceType.Default,
                    new EventSourceId($"source-{i}"),
                    EventStreamType.All,
                    EventStreamId.Default,
                    (ulong)i,
                    CorrelationId.NotSet,
                    tags: null,
                    DateTimeOffset.UtcNow),
                content))
            .ToArray();

        _fakeHandler = new FakeHttpMessageHandler(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        _httpClient = new HttpClient(_fakeHandler)
        {
            BaseAddress = new Uri("https://example.com/webhook")
        };
        _httpClientFactory.Create(_target).Returns(_httpClient);
    }

    async Task Because() => _result = await _mediator.OnNext(_target, _partition, _events);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();

    [Fact] void should_send_all_events() => _fakeHandler.RequestsSent.ShouldEqual(1);
}
