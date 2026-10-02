// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_pii_has_been_erased : given.a_protected_event
{
    void Establish() => _keys.TryGetFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>()).Returns((EncryptionKey?)null);

    async Task Because() => _result = await _command.Handle(_storage, _manager);

    [Fact] void should_not_mistake_erasure_for_empty_content() => _result.Result.ShouldEqual(ContentVerificationResult.Unavailable);
}
