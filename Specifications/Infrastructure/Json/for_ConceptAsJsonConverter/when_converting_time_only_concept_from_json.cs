// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Json.for_ConceptAsJsonConverter;

public class when_converting_time_only_concept_from_json : given.converter_for_converting_from_json<TimeOnlyConcept, TimeOnly>
{
    protected override TimeOnly InputValue => TimeOnly.FromDateTime(DateTime.UtcNow);

    protected override string FormattedInput => $"\"{input:O}\"";

    [Fact] void should_convert_to_correct_time_only() => ShouldConvertToCorrectConcept();
}
