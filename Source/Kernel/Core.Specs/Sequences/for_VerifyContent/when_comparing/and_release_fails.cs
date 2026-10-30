// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_release_fails : given.a_protected_event
{
    void Establish() => _keys.TryGetFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>()).Returns(_encryption.GenerateKey());

    async Task Because() => _result = await Verify();

    [Fact] void should_not_compare_a_fallback_empty_value() => _result.Result.ShouldEqual(ContentVerificationResult.Unavailable);
}
