// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Json.for_ConceptAsJsonConverter;

public class when_converting_byte_concept_from_json : given.converter_for_converting_from_json<ByteConcept, byte>
{
    protected override byte InputValue => 42;

    protected override string FormattedInput => "42";

    [Fact] void should_convert_to_correct_byte() => ShouldConvertToCorrectConcept();
}
