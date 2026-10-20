// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.when_testing_a_webhook;

public class with_event_store_only_hosts : for_WebhookMediator.given.a_store_scoped_delivery
{
    WebhookRegistrar _registrar;
    WebhookTestResult _allowed;
    WebhookTestResult _rejected;

    void Establish() => _registrar = new(
        Substitute.For<IGrainFactory>(),
        Substitute.For<IWebhookDefinitionComparer>(),
        Substitute.For<Security.IEncryption>(),
        Substitute.For<IOAuthClient>(),
        _mediator,
        Options.Create(new ChronicleOptions()),
        Substitute.For<Storage.IStorage>(),
        NullLogger<WebhookRegistrar>.Instance);

    async Task Because()
    {
        var target = new Contracts.Observation.Webhooks.WebhookTarget { Url = _target.Url };
        _allowed = await _registrar.TestWebhook(target, "orders");
        _rejected = await _registrar.TestWebhook(target, "other");
    }

    [Fact] void should_allow_the_registration_probe_for_the_store() => _allowed.Success.ShouldBeTrue();
    [Fact] void should_reject_the_same_probe_for_another_store() => _rejected.Success.ShouldBeFalse();
    [Fact] void should_only_contact_the_target_for_the_allowed_store() => _receiver.Stores.ShouldContainOnly((EventStoreName)"orders");
}
