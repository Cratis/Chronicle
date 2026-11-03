// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber.when_notifying_changes.and_the_model_contains_pii;

public class with_subjects : given.a_change_for_a_model_with_pii
{
    protected override void AddToState(IDictionary<string, object?> state)
    {
        var subjects = new ExpandoObject();
        ((IDictionary<string, object?>)subjects)["name"] = "subject-2";
        state[WellKnownProperties.Subjects] = subjects;
    }

    async Task Because() => await _subscriber.OnNext("the-partition", [_event], new(null));

    [Fact] void should_carry_the_property_subjects_in_the_change() => _change.Subjects["name"].ShouldEqual("subject-2");
    [Fact] void should_keep_the_subjects_on_the_document_for_silos_of_the_previous_release() => _document[WellKnownProperties.Subjects]["name"].GetValue<string>().ShouldEqual("subject-2");
}
