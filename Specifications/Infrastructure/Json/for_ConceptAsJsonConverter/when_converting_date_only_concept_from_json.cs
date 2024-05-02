// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Json.for_ConceptAsJsonConverter;

public class when_converting_date_only_concept_from_json : given.converter_for_converting_from_json<DateOnlyConcept, DateOnly>
{
    protected override DateOnly InputValue => DateOnly.FromDateTime(DateTime.UtcNow);

    protected override string FormattedInput => $"\"{input:O}\"";

    [Fact] void should_convert_to_correct_date_only() => ShouldConvertToCorrectConcept();
}
