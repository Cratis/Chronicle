// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.ReadModels.for_ReadModelChangeBookkeeping.when_reading_lineage;

/// <summary>
/// Without lineage there is nothing to carry, so nothing is allocated for it (Cratis/Chronicle#3588).
/// </summary>
public class and_state_carries_no_lineage : Specification
{
    ExpandoObject _state;
    ExpandoObject _stateWithNullSubjects;
    (string? Subject, IReadOnlyDictionary<string, string>? Subjects) _result;
    (string? Subject, IReadOnlyDictionary<string, string>? Subjects) _resultWithNullSubjects;

    void Establish()
    {
        _state = new ExpandoObject();
        ((IDictionary<string, object?>)_state)["name"] = "value";
        _stateWithNullSubjects = new ExpandoObject();
        ((IDictionary<string, object?>)_stateWithNullSubjects)[Storage.WellKnownProperties.Subjects] = null;
    }

    void Because()
    {
        _result = ReadModelChangeBookkeeping.LineageOf(_state);
        _resultWithNullSubjects = ReadModelChangeBookkeeping.LineageOf(_stateWithNullSubjects);
    }

    [Fact] void should_have_no_subject() => _result.Subject.ShouldBeNull();
    [Fact] void should_have_no_subjects() => _result.Subjects.ShouldBeNull();
    [Fact] void should_have_no_subjects_for_null_subjects() => _resultWithNullSubjects.Subjects.ShouldBeNull();
}
