// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_RotateWebhookSigningKeyValidator.when_validating;

public class with_invalid_overlaps : Specification
{
    double[] _overlaps;
    bool[] _valid;

    void Establish() => _overlaps = [-1, double.NaN, double.PositiveInfinity, double.NegativeInfinity, WebhookSigningKeyOverlap.Maximum.Value.TotalSeconds + 1];

    async Task Because()
    {
        var validator = new RotateWebhookSigningKeyValidator();
        _valid = new bool[_overlaps.Length];
        for (var index = 0; index < _overlaps.Length; index++)
        {
            _valid[index] = (await validator.ValidateAsync(new RotateWebhookSigningKey("store", _overlaps[index]))).IsValid;
        }
    }

    [Fact] void should_reject_negative_overlap() => _valid[0].ShouldBeFalse();
    [Fact] void should_reject_nan() => _valid[1].ShouldBeFalse();
    [Fact] void should_reject_positive_infinity() => _valid[2].ShouldBeFalse();
    [Fact] void should_reject_negative_infinity() => _valid[3].ShouldBeFalse();
    [Fact] void should_reject_overlap_beyond_the_maximum() => _valid[4].ShouldBeFalse();
}
