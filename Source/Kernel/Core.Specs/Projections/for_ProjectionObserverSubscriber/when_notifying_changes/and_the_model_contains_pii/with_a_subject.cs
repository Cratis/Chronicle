// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber.when_notifying_changes.and_the_model_contains_pii;

public class with_a_subject : given.a_change_for_a_model_with_pii
{
    protected override void AddToState(IDictionary<string, object?> state) => state[WellKnownProperties.Subject] = "subject-1";

    async Task Because() => await _subscriber.OnNext("the-partition", [_event], new(null));

    [Fact] void should_carry_the_subject_in_the_change() => _change.Subject.ShouldEqual("subject-1");
    [Fact] void should_not_carry_property_subjects_in_the_change() => _change.Subjects.ShouldBeNull();
    [Fact] void should_keep_the_subject_on_the_document_for_silos_of_the_previous_release() => _document[WellKnownProperties.Subject].GetValue<string>().ShouldEqual("subject-1");
}
