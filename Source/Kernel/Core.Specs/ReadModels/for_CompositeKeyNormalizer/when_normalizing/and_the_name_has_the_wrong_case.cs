// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_CompositeKeyNormalizer.when_normalizing;

public class and_the_name_has_the_wrong_case : given.a_composite_key_schema
{
    CompositeKeyNormalization _result;

    void Because()
    {
        var parts = ValidParts();
        parts["Currency"] = parts["currency"];
        parts.Remove("currency");
        _result = CompositeKeyNormalizer.Normalize(_schema, parts, _typeFormats);
    }

    [Fact] void should_refuse_the_key() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_explain_why() => _result.Message.ShouldContain("unknown parts: Currency");
}
