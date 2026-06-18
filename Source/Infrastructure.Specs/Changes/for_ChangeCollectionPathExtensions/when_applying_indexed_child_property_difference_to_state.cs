// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes.for_ChangeCollectionPathExtensions;

public class when_applying_indexed_child_property_difference_to_state : Specification
{
    ExpandoObject _state;
    ExpandoObject _result;
    Guid _candidateId;

    void Establish()
    {
        _candidateId = Guid.NewGuid();

        dynamic candidate = new ExpandoObject();
        candidate.candidateId = _candidateId;
        candidate.name = "Ada";
        candidate.isCustomerSigned = false;
        candidate.isPartnerSigned = false;

        dynamic state = new ExpandoObject();
        state.candidates = new List<object> { candidate };
        _state = state;
    }

    void Because()
    {
        var propertyPath = new PropertyPath("[candidates].isCustomerSigned");
        var arrayIndexers = new ArrayIndexers(
        [
            new ArrayIndexer(
                new PropertyPath("[candidates]"),
                new PropertyPath("candidateId"),
                _candidateId)
        ]);

        var propertiesChanged = new PropertiesChanged<ExpandoObject>(
            _state,
            [new PropertyDifference(propertyPath, false, true, arrayIndexers)]);

        _result = propertiesChanged.ApplyToStateWithoutChildOperationConflicts(_state, new HashSet<PropertyPath>());
    }

    [Fact] void should_keep_one_candidate() => Candidates.Length.ShouldEqual(1);
    [Fact] void should_preserve_child_identifier() => Candidate["candidateId"].ShouldEqual(_candidateId);
    [Fact] void should_preserve_existing_child_field() => Candidate["name"].ShouldEqual("Ada");
    [Fact] void should_apply_changed_child_field() => Candidate["isCustomerSigned"].ShouldEqual(true);
    [Fact] void should_preserve_unrelated_child_field() => Candidate["isPartnerSigned"].ShouldEqual(false);

    ExpandoObject[] Candidates => ((IEnumerable<object>)((IDictionary<string, object?>)_result)["candidates"]!).Cast<ExpandoObject>().ToArray();
    IDictionary<string, object?> Candidate => Candidates[0];
}
