// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Json.for_ConceptAsJsonConverter;

public class when_converting_sbyte_concept_to_json : given.converter_for_converting_to_json<SByteConcept, sbyte>
{
    protected override sbyte Expected => 42;

    protected override string FormattedExpected => "42";

    [Fact] void should_convert_to_correct_byte() => ShouldConvertToJson();
}
