// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_RotateWebhookSigningKeyValidator.when_validating;

public class with_valid_overlaps : Specification
{
    bool _defaultValid;
    bool _zeroValid;
    bool _maximumValid;

    async Task Because()
    {
        var validator = new RotateWebhookSigningKeyValidator();
        _defaultValid = (await validator.ValidateAsync(new RotateWebhookSigningKey("store"))).IsValid;
        _zeroValid = (await validator.ValidateAsync(new RotateWebhookSigningKey("store", 0))).IsValid;
        _maximumValid = (await validator.ValidateAsync(new RotateWebhookSigningKey("store", WebhookSigningKeyOverlap.Maximum.Value.TotalSeconds))).IsValid;
    }

    [Fact] void should_allow_the_default_overlap() => _defaultValid.ShouldBeTrue();
    [Fact] void should_allow_zero_overlap() => _zeroValid.ShouldBeTrue();
    [Fact] void should_allow_the_maximum_overlap() => _maximumValid.ShouldBeTrue();
}
