// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Json.for_ConceptAsJsonConverter;

public class when_converting_guid_concept_from_json : given.converter_for_converting_from_json<GuidConcept, Guid>
{
    protected override Guid InputValue => Guid.NewGuid();

    protected override string FormattedInput => $"\"{input}\"";

    [Fact] void should_convert_to_correct_guid() => ShouldConvertToCorrectConcept();
}
