// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookAdmission.when_writing;

public class with_observer_history : given.an_envelope_write
{
    void Establish() => _storage.GetEventStore(Arg.Any<EventStoreName>()).Observers.Has(Arg.Any<ObserverId>()).Returns(true);

    async Task Because() => _error = await Catch.Exception(Write);

    [Fact] void should_refuse_the_identifier() => _error.ShouldBeOfExactType<EnvelopeWebhookIdentifierHasObserverHistory>();
    [Fact] void should_not_build_changes() => _comparisons.ShouldEqual(0);
    [Fact] void should_not_append() => AppendCount.ShouldEqual(0);
}
