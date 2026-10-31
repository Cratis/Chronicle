// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventSequences.Idempotency.for_AppendFingerprint;

public class when_the_identity_differs : given.an_append_intent
{
    void Establish()
    {
        Protect();
        _input = _input with { Identity = "append-key:other" };
    }
    void Because() => _result = Compute(_input, Use("name", ProtectionKind.Pii, _key));
    [Fact] void should_keep_the_plain_part() => _result.Plain.ShouldEqual(_original.Plain);
    [Fact] void should_bind_the_protected_part_to_the_identity() => _result.ProtectedParts[0].Hash.ShouldNotEqual(_original.ProtectedParts[0].Hash);
}
