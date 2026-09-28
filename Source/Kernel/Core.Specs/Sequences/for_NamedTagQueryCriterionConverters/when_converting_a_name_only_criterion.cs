// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_NamedTagQueryCriterionConverters;

public class when_converting_a_name_only_criterion : Specification
{
    NamedTagQueryCriterion _criterion;

    void Because()
    {
        using var stream = new MemoryStream();
        ProtoBuf.Serializer.Serialize(stream, new Contracts.Sequences.NamedTagQueryCriterion
        {
            Name = "account",
            AnyValue = true,
            Values = []
        });
        stream.Position = 0;
        _criterion = ProtoBuf.Serializer.Deserialize<Contracts.Sequences.NamedTagQueryCriterion>(stream).ToApi();
    }

    [Fact] void should_match_any_value_for_the_name() => _criterion.Values.ShouldBeNull();
    [Fact] void should_preserve_the_explicit_wildcard() => _criterion.AnyValue.ShouldBeTrue();
    [Fact] void should_keep_the_name() => _criterion.Name.ShouldEqual("account");
}
