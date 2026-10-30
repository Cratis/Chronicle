// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_CompositeKeyNormalizer.when_normalizing;

public class and_a_part_is_null : given.a_composite_key_schema
{
    CompositeKeyNormalization _result;

    void Because()
    {
        var parts = ValidParts();
        parts["currency"] = "null";
        _result = CompositeKeyNormalizer.Normalize(_schema, parts, _typeFormats);
    }

    [Fact] void should_be_valid() => _result.IsValid.ShouldBeTrue();
    [Fact] void should_keep_the_part_as_null() => ((IDictionary<string, object?>)_result.Key!)["currency"].ShouldBeNull();
}
