// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Observation.Webhooks.for_RemoveWebhook.when_validating;

public class and_all_values_are_provided : Specification
{
    readonly CommandScenario<RemoveWebhook> _scenario = new();
    CommandResult _result;

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        storage.HasEventStore(Arg.Any<EventStoreName>()).Returns(true);
        storage.GetEventStore(Arg.Any<EventStoreName>()).Webhooks.Has(Arg.Any<WebhookId>()).Returns(true);
        _scenario.Services.AddSingleton(storage);
    }

    async Task Because() => _result = await _scenario.Validate(new RemoveWebhook("some-event-store", "some-webhook"));

    [Fact] void should_be_valid() => _result.ShouldBeValid();
}
