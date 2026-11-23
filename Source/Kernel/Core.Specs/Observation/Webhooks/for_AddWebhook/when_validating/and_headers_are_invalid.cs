// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Contracts.Security;
using Cratis.Chronicle.Observation.Webhooks.Validation;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Storage;
using FluentValidation.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Observation.Webhooks.for_AddWebhook.when_validating;

public class and_headers_are_invalid : Helpers.for_CustomHttpHeaders.given.invalid_headers
{
    AddWebhookValidator _validator;
    IWebhookMediator _mediator;
    ValidationResult[] _results;

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        storage.GetEventStore("store").Webhooks.GetAll().Returns([]);
        var policy = Substitute.For<IWebhookTargetPolicy>();
        _mediator = Substitute.For<IWebhookMediator>();
        var registrar = new WebhookRegistrar(Substitute.For<IGrainFactory>(), Substitute.For<IWebhookDefinitionComparer>(), Substitute.For<IEncryption>(), Substitute.For<IOAuthClient>(), _mediator, Options.Create(new ChronicleOptions()), storage, NullLogger<WebhookRegistrar>.Instance, policy);
        _validator = new(registrar, storage, policy, Substitute.For<IManagedIdentityTokens>());
    }

    async Task Because() => _results = await Task.WhenAll(_invalidHeaders.Select(headers => _validator.ValidateAsync(new AddWebhook("store", "webhook", "https://receiver.example", "event-log", [], AuthorizationType.None, "", "", "", "", "", "", headers, true, true))));

    [Fact] void should_reject_every_invalid_header() => _results.All(result => !result.IsValid).ShouldBeTrue();
    [Fact] void should_identify_the_headers() => _results.All(result => result.Errors.Exists(error => error.PropertyName.StartsWith("Headers", StringComparison.Ordinal))).ShouldBeTrue();
    [Fact] void should_not_include_the_value_in_validation_errors() => _results.All(result => !result.ToString().Contains(Secret)).ShouldBeTrue();
    [Fact] void should_not_probe_the_endpoint() => _mediator.ReceivedCalls().ShouldBeEmpty();
}
