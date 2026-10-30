// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.ReadModels.for_CompositeKeyNormalizer.when_normalizing;

public class and_the_read_model_has_a_scalar_key : Specification
{
    CompositeKeyNormalization _result;

    void Because() => _result = CompositeKeyNormalizer.Normalize(
        JsonSchema.FromJson("""{"type":"object","properties":{"id":{"type":"string","format":"guid"}}}"""),
        new Dictionary<string, string> { ["id"] = "\"x\"" },
        new TypeFormats());

    [Fact] void should_refuse_the_key() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_say_there_is_no_composite_key() => _result.Message.ShouldContain("composite key");
}
