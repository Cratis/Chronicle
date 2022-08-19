// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Json.for_ConceptAsJsonConverter;

public class when_converting_date_only_concept_to_json : given.converter_for_converting_to_json<DateOnlyConcept, DateOnly>
{
    protected override DateOnly Expected => DateOnly.FromDateTime(DateTime.UtcNow);
    protected override string FormattedExpected => $"\"{input.Value:O}\"";

    [Fact] void should_convert_to_correct_date_only() => ShouldConvertToJson();
}
