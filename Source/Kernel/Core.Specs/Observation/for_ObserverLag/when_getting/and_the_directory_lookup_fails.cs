// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverLag.when_getting;

public class and_the_directory_lookup_fails : given.an_observer_lag_query
{
    ObserverLag _result;

    void Establish()
    {
#pragma warning disable CA2012 // NSubstitute captures the configured call; each invocation returns a fresh value task.
        _directory.GetActivationAddress(_grain).Returns(_ => ValueTask.FromException<SiloAddress?>(new TimeoutException()));
#pragma warning restore CA2012
    }

    async Task Because() => _result = await Query();

    [Fact] void should_report_no_sample() => _result.Cause.ShouldEqual(ObserverLagCause.NoSample);
    [Fact] async Task should_not_wake_the_observer() => await _grain.DidNotReceive().GetInformation();
    [Fact] async Task should_not_check_dormancy() => await _dormant.DidNotReceive().Find(_key);
}
