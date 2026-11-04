// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerPipeline.when_reducing;

/// <summary>The compliance subject lineage of the stored state travels in the change, outside the document.</summary>
public class and_the_state_has_subject_lineage : given.all_dependencies
{
    ReadModelChangeNotification _result;
    ExpandoObject _returnedState;

    void Establish()
    {
        _returnedState = new ExpandoObject();
        var values = (IDictionary<string, object?>)_returnedState;
        values["value"] = "after";
        var subjects = new ExpandoObject();
        ((IDictionary<string, object?>)subjects)["value"] = "subject-2";
        values[WellKnownProperties.Subjects] = subjects;

        var initial = new ExpandoObject();
        ((IDictionary<string, object?>)initial)["value"] = "before";
        _sink.FindOrDefault(Arg.Any<Concepts.Keys.Key>()).Returns(initial);
        _objectComparer.Compare(Arg.Any<ExpandoObject>(), Arg.Any<ExpandoObject>(), out Arg.Any<IEnumerable<Changes.PropertyDifference>>()).Returns(call =>
        {
            call[2] = new Changes.PropertyDifference[] { new("value", "before", "after") };
            return false;
        });
    }

    async Task Because() => _result = await _pipeline.Reduce(CreateContext(EventSourceIdValue), CreateReducer(_returnedState));

    [Fact] void should_keep_the_subject_on_the_document_for_silos_of_the_previous_release() => _result.ReadModel[WellKnownProperties.Subject].GetValue<string>().ShouldEqual(EventSourceIdValue);
    [Fact] void should_keep_the_subjects_on_the_document_for_silos_of_the_previous_release() => _result.ReadModel[WellKnownProperties.Subjects]["value"].GetValue<string>().ShouldEqual("subject-2");
    [Fact] void should_carry_the_subject_in_the_change() => _result.Change.Subject.ShouldEqual(EventSourceIdValue);
    [Fact] void should_carry_the_property_subjects_in_the_change() => _result.Change.Subjects["value"].ShouldEqual("subject-2");
}
