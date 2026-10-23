// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.ReadModels.for_CompositeKeyNormalizer.when_normalizing;

public class and_a_part_is_of_a_type_that_cannot_be_a_key : Specification
{
    CompositeKeyNormalization _result;

    void Because() => _result = CompositeKeyNormalizer.Normalize(
        JsonSchema.FromJson("""{"type":"object","properties":{"id":{"type":"object","properties":{"tags":{"type":"array","items":{"type":"string"}}}}}}"""),
        new Dictionary<string, string> { ["tags"] = "[\"a\"]" },
        new TypeFormats());

    [Fact] void should_refuse_the_key() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_name_the_part() => _result.Message.ShouldContain("'tags'");
}
