// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Cratis.Json.for_EnumerableModelWithIdToConceptOrPrimitiveEnumerableConverter;

public class when_converting_from_json_with_array_values : Specification
{
    EnumerableModelWithIdToConceptOrPrimitiveEnumerableConverter<IEnumerable<GuidConcept>, GuidConcept> converter;

    string first_guid;
    string second_guid;
    IEnumerable<GuidConcept> result;

    void Establish()
    {
        converter = new();
        first_guid = Guid.NewGuid().ToString();
        second_guid = Guid.NewGuid().ToString();
    }

    void Because()
    {
        var json = $"[\"{first_guid}\",\"{second_guid}\"]";
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json).AsSpan());
        reader.Read();
        result = converter.Read(ref reader, typeof(IEnumerable<GuidConcept>), default);
    }

    [Fact] void should_hold_first_guid() => result.ToArray()[0].ToString().ShouldEqual(first_guid);
    [Fact] void should_hold_second_guid() => result.ToArray()[1].ToString().ShouldEqual(second_guid);
}
