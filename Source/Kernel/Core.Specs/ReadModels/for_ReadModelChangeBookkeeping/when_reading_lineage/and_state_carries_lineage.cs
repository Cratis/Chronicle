// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.ReadModels.for_ReadModelChangeBookkeeping.when_reading_lineage;

public class and_state_carries_lineage : Specification
{
    ExpandoObject _state;
    (string? Subject, IReadOnlyDictionary<string, string>? Subjects) _result;

    void Establish()
    {
        _state = new ExpandoObject();
        var dictionary = (IDictionary<string, object?>)_state;
        dictionary[Storage.WellKnownProperties.Subject] = "subject";
        dictionary[Storage.WellKnownProperties.Subjects] = new Dictionary<string, object?> { ["name"] = "other" };
    }

    void Because() => _result = ReadModelChangeBookkeeping.LineageOf(_state);

    [Fact] void should_have_the_subject() => _result.Subject.ShouldEqual("subject");
    [Fact] void should_have_the_property_subject() => _result.Subjects!["name"].ShouldEqual("other");
}
