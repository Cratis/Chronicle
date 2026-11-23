// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Contracts.Observation.Webhooks;
using Cratis.Chronicle.Observation.Webhooks.Validation;
using FluentValidation.Results;

namespace Cratis.Chronicle.Observation.Webhooks.for_AddWebhooks.when_validating;

public class and_headers_are_invalid : Helpers.for_CustomHttpHeaders.given.invalid_headers
{
    AddWebhooksValidator _validator;
    ValidationResult[] _results;

    void Establish()
    {
        var policy = Substitute.For<IWebhookTargetPolicy>();
        policy.Evaluate(Arg.Any<string>(), Arg.Any<EventStoreName?>(), Arg.Any<CancellationToken>()).Returns(WebhookTargetValidation.Allowed);
        _validator = new(policy);
    }

    async Task Because() => _results = await Task.WhenAll(_invalidHeaders.Select(headers => _validator.ValidateAsync(new AddWebhooks("store",
    [
        new WebhookDefinition
        {
            Identifier = "webhook",
            EventSequenceId = "event-log",
            Target = new() { Url = "https://receiver.example", Headers = headers }
        }
    ]))));

    [Fact] void should_reject_every_invalid_header() => _results.All(result => !result.IsValid).ShouldBeTrue();
    [Fact] void should_identify_the_headers() => _results.All(result => result.Errors.Exists(error => error.PropertyName.Contains("Headers", StringComparison.Ordinal))).ShouldBeTrue();
    [Fact] void should_not_include_the_value_in_validation_errors() => _results.All(result => !result.ToString().Contains(Secret)).ShouldBeTrue();
}
