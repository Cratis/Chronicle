// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;
using ObjectsComparer;

namespace Cratis.Changes.for_PropertyDifference
{
    public class when_original_and_changed_value_is_string_but_target_type_is_concept : Specification
    {
        const string original_value = "c679e841-6905-4d24-ba9e-f171f5faa44f";
        const string changed_value = "07cfdd41-2dd1-4d5b-a2d2-c11e28fc7017";

        record GuidConcept(Guid Value) : ConceptAs<Guid>(Value);
        record ObjectForComparison(GuidConcept GuidConcept);

        PropertyDifference<ObjectForComparison> result;

        void Because() => result = new PropertyDifference<ObjectForComparison>(new(new(Guid.Empty)), new(new(Guid.Empty)), new Difference("GuidConcept", original_value, changed_value, DifferenceTypes.ValueMismatch));

        [Fact] void should_have_the_correct_original_value() => ((GuidConcept)result.Original).Value.ShouldEqual(Guid.Parse(original_value));
        [Fact] void should_have_the_correct_changed_value() => ((GuidConcept)result.Changed).Value.ShouldEqual(Guid.Parse(changed_value));
    }
}
