// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Properties;
using Catch = Cratis.Monads.Catch;

namespace Cratis.Chronicle.Grains.Observation.Webhooks.for_WebhookMediator.when_calling_on_next;

public class with_failed_webhook_request : given.a_webhook_mediator
{
    WebhookTarget _target;
    Key _partition;
    IEnumerable<AppendedEvent> _events;
    Catch _result;
    HttpClient _httpClient;
    Exception _exception;
    FakeHttpMessageHandler _fakeHandler;

    void Establish()
    {
        _partition = new Key("test-partition", ArrayIndexers.NoIndexers);
        _target = new WebhookTarget(
            new WebhookTargetUrl("https://example.com/webhook"),
            WebhookAuthorization.None,
            new Dictionary<string, string>());

        _events = [];

        _exception = new HttpRequestException("Connection failed");
        _fakeHandler = new FakeHttpMessageHandler(_exception);
        _httpClient = new HttpClient(_fakeHandler)
        {
            BaseAddress = new Uri("https://example.com/webhook")
        };
        _httpClientFactory.Create(_target).Returns(_httpClient);
    }

    async Task Because() => _result = await _mediator.OnNext(_target, _partition, _events);

    [Fact] void should_fail() => _result.IsSuccess.ShouldBeFalse();

    [Fact] void should_capture_exception()
    {
        _result.TryGetException(out var exception).ShouldBeTrue();
        exception.ShouldEqual(_exception);
    }
}
