// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.EventSequences.Idempotency.for_AppendFingerprint;

public class when_two_keys_protect_the_same_values : given.an_append_intent
{
    void Establish() => Protect();
    void Because() => _result = Compute(_input, Use("name", ProtectionKind.Pii, new EncryptionKey([7, 8, 9], [10, 11, 12])));
    [Fact] void should_preserve_the_plain_part() => _result.Plain.ShouldEqual(_original.Plain);
    [Fact] void should_not_reveal_equality_between_keys() => _result.ProtectedParts[0].Hash.ShouldNotEqual(_original.ProtectedParts[0].Hash);
}
