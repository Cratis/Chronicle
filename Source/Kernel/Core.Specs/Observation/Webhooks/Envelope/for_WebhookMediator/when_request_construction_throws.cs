// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Webhooks.Envelope.for_WebhookMediator;

public class when_request_construction_throws : given.a_delivery_request
{
    WebhookDeliveryResult _result;
    Exception _error;

    async Task Because()
    {
        _error = new InvalidOperationException("Request construction failed");
        var factory = Substitute.For<IWebhookHttpClientFactory>();
        factory.Create(_request.Target, _request.AccessToken).Returns(new HttpClient { BaseAddress = new("https://example.com/webhook") });
        var resolver = Substitute.For<IJsonTypeInfoResolver>();
        resolver.GetTypeInfo(typeof(EventsToObserve), Arg.Any<JsonSerializerOptions>()).Returns(_ => throw _error);
        _options.TypeInfoResolver = resolver;
        _result = await new WebhookMediator(factory, _options).Deliver(_request.EventStore, _request.Target, _request.Partition, _request.Events);
    }

    [Fact] void should_return_the_failure_instead_of_throwing() => _result.Error.ShouldEqual(_error);
}
