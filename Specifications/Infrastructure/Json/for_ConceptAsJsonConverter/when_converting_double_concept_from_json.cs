// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Json.for_ConceptAsJsonConverter;

public class when_converting_double_concept_from_json : given.converter_for_converting_from_json<DoubleConcept, double>
{
    protected override double InputValue => 42.43;

    protected override string FormattedInput => "42.43";

    [Fact] void should_convert_to_correct_double() => ShouldConvertToCorrectConcept();
}
