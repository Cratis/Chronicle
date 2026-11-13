// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Services.Observation.for_SubscriptionCheckSchedule;

/// <summary>
/// A revise interval of zero would make every keeper a tight loop of grain calls.
/// </summary>
public class when_created_with_a_revise_interval_of_zero : Specification
{
    TimeSpan _revise;

    void Because() => _revise = SubscriptionCheckSchedule.From(new ChronicleOptions { ConnectedClients = new() { ReviseIntervalSeconds = 0 } }).Revise;

    [Fact] void should_check_no_more_often_than_every_second() => _revise.ShouldEqual(TimeSpan.FromSeconds(1));
}
