// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Json.for_ConceptAsJsonConverter;

public class when_converting_boolean_concept_to_json : given.converter_for_converting_to_json<BooleanConcept, bool>
{
    protected override bool Expected => true;

    protected override string FormattedExpected => "true";

    [Fact] void should_convert_to_correct_guid() => ShouldConvertToJson();
}
