// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Json.for_ConceptAsJsonConverter;

public class when_converting_time_only_concept_to_json : given.converter_for_converting_to_json<TimeOnlyConcept, TimeOnly>
{
    protected override TimeOnly Expected => TimeOnly.FromDateTime(DateTime.UtcNow);

    protected override string FormattedExpected => $"\"{input.Value:O}\"";

    [Fact] void should_convert_to_correct_time_only() => ShouldConvertToJson();
}
