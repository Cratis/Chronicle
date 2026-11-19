// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Contracts.Observation.Webhooks;
using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.Observation.Webhooks.for_AddWebhooksValidator.when_validating;

public class with_signing_modes : Specification
{
    AddWebhooksValidator _validator;

    void Establish()
    {
        var policy = Substitute.For<IWebhookTargetPolicy>();
        policy.Evaluate(Arg.Any<string>(), Arg.Any<EventStoreName?>(), Arg.Any<CancellationToken>()).Returns(new WebhookTargetValidation(true, string.Empty));
        _validator = new(policy);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(99, false)]
    async Task should_accept_only_supported_registration_modes(int mode, bool valid)
    {
        var result = await _validator.ValidateAsync(new AddWebhooks("store", [new WebhookDefinition
        {
            Identifier = "webhook",
            EventSequenceId = "log",
            Signing = (WebhookSigningMode)mode,
            Target = new() { Url = "https://example.com/webhook" }
        }]));
        result.IsValid.ShouldEqual(valid);
        if (!valid) result.Errors.Exists(_ => _.PropertyName.Contains("Signing", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
