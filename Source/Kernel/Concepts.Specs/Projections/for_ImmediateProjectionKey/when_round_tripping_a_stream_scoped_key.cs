// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Concepts.Projections.for_ImmediateProjectionKey;

public class when_round_tripping_a_stream_scoped_key : Specification
{
    ImmediateProjectionKey _key;
    ImmediateProjectionKey _result;

    void Establish() => _key = new(
        "projection",
        "store",
        "namespace",
        EventSequenceId.Log,
        "stream#key",
        (ProjectionSessionId)Guid.Parse("cb700f32-011f-41f9-a9b8-df71811394cd"),
        new("source#id", "source%type", "stream#type", "stream#key"));

    void Because() => _result = ImmediateProjectionKey.Parse(_key.ToString());

    [Fact] void should_preserve_every_dimension() => _result.ShouldEqual(_key);
}
