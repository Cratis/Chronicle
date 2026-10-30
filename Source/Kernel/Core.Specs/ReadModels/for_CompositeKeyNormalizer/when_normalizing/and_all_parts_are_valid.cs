// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.ReadModels.for_CompositeKeyNormalizer.when_normalizing;

public class and_all_parts_are_valid : given.a_composite_key_schema
{
    CompositeKeyNormalization _result;
    IDictionary<string, object?> _key;

    void Because()
    {
        _result = CompositeKeyNormalizer.Normalize(_schema, ValidParts(), _typeFormats);
        _key = _result.Key!;
    }

    [Fact] void should_be_valid() => _result.IsValid.ShouldBeTrue();
    [Fact] void should_type_the_string() => _key["currency"].ShouldEqual("EUR");
    [Fact] void should_type_the_whole_number() => _key["invoice"].ShouldEqual(42L);
    [Fact] void should_type_the_guid() => _key["reference"].ShouldEqual(Guid.Parse("0d0b9d4c-6e0a-4c4f-8c1b-2b6e3f6b1a11"));
    [Fact] void should_type_the_date() => _key["issued"].ShouldEqual(new DateTimeOffset(2026, 10, 23, 10, 0, 0, TimeSpan.FromHours(2)));
    [Fact] void should_type_the_decimal() => _key["amount"].ShouldEqual(1.5m);
    [Fact] void should_type_the_boolean() => _key["paid"].ShouldEqual(true);
    [Fact]
    void should_encode_to_the_same_value_as_the_typed_parts() =>
        CompositeKeyEncoding.Encode(_key).ShouldEqual(CompositeKeyEncoding.Encode(new Dictionary<string, object?>
        {
            ["currency"] = "EUR",
            ["invoice"] = 42L,
            ["reference"] = Guid.Parse("0d0b9d4c-6e0a-4c4f-8c1b-2b6e3f6b1a11"),
            ["issued"] = new DateTimeOffset(2026, 10, 23, 8, 0, 0, TimeSpan.Zero),
            ["amount"] = 1.5m,
            ["paid"] = true
        }));
}
