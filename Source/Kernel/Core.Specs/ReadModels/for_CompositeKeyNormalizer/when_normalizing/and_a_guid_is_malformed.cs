// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_CompositeKeyNormalizer.when_normalizing;

public class and_a_guid_is_malformed : given.a_composite_key_schema
{
    CompositeKeyNormalization _result;

    void Because()
    {
        var parts = ValidParts();
        parts["reference"] = "\"not-a-guid\"";
        _result = CompositeKeyNormalizer.Normalize(_schema, parts, _typeFormats);
    }

    [Fact] void should_refuse_the_key() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_explain_why() => _result.Message.ShouldContain("'reference'");
}
