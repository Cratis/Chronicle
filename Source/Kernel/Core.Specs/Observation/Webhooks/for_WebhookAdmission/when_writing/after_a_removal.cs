// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookAdmission.when_writing;

public class after_a_removal : given.an_envelope_write
{
    void Establish() => ALogWithLastLifecycleEvent(ALifecycleEvent<WebhookRemoved>(7));

    async Task Because() => _error = await Catch.Exception(Write);

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_fence_against_the_removal() => ScopedAppends.Single().SequenceNumber.ShouldEqual((EventSequenceNumber)7);
    [Fact] void should_mark_the_request_as_admitted() => _context.Admitted.ShouldContain("webhook");
}
