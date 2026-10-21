// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Idempotency.for_AppendFingerprint;

public class when_the_generation_differs : given.an_append_intent
{
    void Establish() => _input = _input with { EventType = _input.EventType with { Generation = 2 } };
    void Because() => _result = Compute(_input);
    [Fact] void should_change_the_plain_part() => _result.Plain.ShouldNotEqual(_original.Plain);
}
