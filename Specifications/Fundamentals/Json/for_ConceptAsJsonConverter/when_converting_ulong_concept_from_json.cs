// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Json.for_ConceptAsJsonConverter;

public class when_converting_ulong_concept_from_json : given.converter_for_converting_from_json<ULongConcept, ulong>
{
    protected override ulong InputValue => 42;

    protected override string FormattedInput => "42";

    [Fact] void should_convert_to_correct_ulong() => ShouldConvertToCorrectConcept();
}
