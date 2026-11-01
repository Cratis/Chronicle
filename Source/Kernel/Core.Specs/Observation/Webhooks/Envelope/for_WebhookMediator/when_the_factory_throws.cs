// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.Envelope.for_WebhookMediator;

public class when_the_factory_throws : given.a_delivery_request
{
    WebhookDeliveryResult _result;
    Exception _error;

    async Task Because()
    {
        _error = new InvalidOperationException("Factory failed");
        var factory = Substitute.For<IWebhookHttpClientFactory>();
        factory.Create(_request.Target, _request.AccessToken).Returns(_ => throw _error);
        _result = await new WebhookMediator(factory, _options).Deliver(_request.EventStore, _request.Target, _request.Partition, _request.Events);
    }

    [Fact] void should_return_the_failure_instead_of_throwing() => _result.Error.ShouldEqual(_error);
}
