// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.EventSequences.Idempotency.for_AppendFingerprint;

public class when_properties_are_reordered : given.an_append_intent
{
    void Establish() => _input = _input with { Content = JsonNode.Parse("""{"count":1,"name":"Alice"}""")!.AsObject() };
    void Because() => _result = Compute(_input);
    [Fact] void should_preserve_the_fingerprint() => _result.Format().ShouldEqual(_original.Format());
}
