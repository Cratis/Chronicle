// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation.Webhooks.Validation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ContractWebhookDefinition = Cratis.Chronicle.Contracts.Observation.Webhooks.WebhookDefinition;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.when_adding;

public class with_an_unapproved_managed_identity : given.a_webhook_registrar
{
    Exception? _error;

    void Establish()
    {
        var options = Options.Create(new ChronicleOptions
        {
            Webhooks = new()
            {
                AllowedHosts = ["receiver.example"],
                ManagedIdentity = new() { AllowedScopes = ["api://approved/.default"], AllowedClientIds = ["client"] }
            }
        });
        _registrar = CreateRegistrar(new WebhookTargetPolicy(options, Substitute.For<IHostEnvironment>(), Substitute.For<IWebhookHostResolver>(), NullLogger<WebhookTargetPolicy>.Instance));
    }

    async Task Because()
    {
        var requested = new ContractWebhookDefinition
        {
            Identifier = "managed",
            EventSequenceId = WellKnownConstants.EventSequences.EventLog.ToString(),
            Target = new()
            {
                Url = "https://receiver.example",
                ManagedIdentity = new() { ClientId = "client", Scope = "api://receiver/.default" }
            }
        };
        _error = await Catch.Exception(async () => await _registrar.Add("orders", [new() { Identifier = "first", EventSequenceId = requested.EventSequenceId, Target = new() { Url = "https://receiver.example" } }, requested]));
    }

    [Fact] void should_reject_an_unapproved_scope_through_bulk_registration() => _error.ShouldBeOfExactType<ManagedIdentityWebhookNotAllowed>();
    [Fact] void should_not_start_registering_any_webhook() => _grainFactory.ReceivedCalls().ShouldBeEmpty();
}
