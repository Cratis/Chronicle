// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber.when_notifying_changes.and_the_model_contains_pii;

public class with_undeclared_metadata : given.a_change_for_a_model_with_pii
{
    async Task Because() => await _subscriber.OnNext("the-partition", [_event], new(null));

    [Fact] void should_not_carry_the_last_handled_event_sequence_number_on_the_document() => _document.ContainsKey(WellKnownProperties.LastHandledEventSequenceNumber).ShouldBeFalse();
    [Fact] void should_carry_the_event_sequence_number_in_the_change() => _change.EventSequenceNumber.Value.ShouldEqual(42UL);
    [Fact] void should_keep_the_declared_properties() => _document["name"].GetValue<string>().ShouldEqual("Ada Lovelace");
}
