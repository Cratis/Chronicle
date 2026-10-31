// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Concepts.Projections.for_ImmediateProjectionKey;

public class when_round_tripping_optional_scope_dimensions : Specification
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void should_preserve_optional_source_type_and_session(bool hasSourceType, bool hasSession)
    {
        var key = new ImmediateProjectionKey(
            "projection",
            "store",
            "namespace",
            EventSequenceId.Log,
            "stream-id",
            hasSession ? (ProjectionSessionId)Guid.Parse("cb700f32-011f-41f9-a9b8-df71811394cd") : null,
            new("source-id", hasSourceType ? (EventSourceType)"source-type" : null, "stream-type", "stream-id"));
        ImmediateProjectionKey.Parse(key.ToString()).ShouldEqual(key);
    }
}
