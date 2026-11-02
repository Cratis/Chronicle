// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookAdmission.when_writing;

public class with_a_pending_registration : given.an_envelope_write
{
    void Establish() => ALogWithLastLifecycleEvent(ALifecycleEvent<WebhookAdded>(3));

    async Task Because() => _error = await Catch.Exception(Write);

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<WebhookLifecycleChangedDuringRegistration>();
    [Fact] void should_not_build_changes() => _comparisons.ShouldEqual(0);
    [Fact] void should_not_append() => AppendCount.ShouldEqual(0);
}
