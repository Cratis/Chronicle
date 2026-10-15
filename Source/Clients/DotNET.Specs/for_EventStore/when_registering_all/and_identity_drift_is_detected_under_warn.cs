// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_EventStore.when_registering_all;

public class and_identity_drift_is_detected_under_warn : given.an_event_store_with_identity_drift
{
    async Task Because() => _exception = await Catch.Exception(_eventStore.RegisterAll);

    [Fact] void should_continue_registration() => _exception.ShouldBeNull();
    [Fact] void should_report_the_finding() => _eventStore.Registration.IdentityDrift.ShouldContainOnly(ExpectedDrift);
    [Fact] void should_register_the_event_types() => _eventStore.EventTypes.Received(1).Register();
}
