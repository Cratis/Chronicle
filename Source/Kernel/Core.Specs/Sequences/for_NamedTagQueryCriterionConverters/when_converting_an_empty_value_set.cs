// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_NamedTagQueryCriterionConverters;

public class when_converting_an_empty_value_set : Specification
{
    Exception _exception;

    void Because()
    {
        using var stream = new MemoryStream();
        ProtoBuf.Serializer.Serialize(stream, new Contracts.Sequences.NamedTagQueryCriterion
        {
            Name = "account",
            Values = []
        });
        stream.Position = 0;
        var wireCriterion = ProtoBuf.Serializer.Deserialize<Contracts.Sequences.NamedTagQueryCriterion>(stream);
        _exception = Catch.Exception(() => wireCriterion.ToApi());
    }

    [Fact] void should_refuse_a_value_set_without_values() => _exception.ShouldBeOfExactType<Storage.EventSequences.InvalidNamedTagCriterion>();
}
