// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Observation.for_WebhookProbeSchedule;

public class when_calculating_the_delay_cap : Specification
{
    DateTimeOffset _due;

    void Because() => _due = WebhookProbeSchedule.NextDue(new WebhookProbe(), 100000, 1, DateTimeOffset.UnixEpoch);

    [Fact] void should_use_the_expected_deadline() => _due.ShouldEqual(DateTimeOffset.UnixEpoch.AddSeconds(3600));
}
