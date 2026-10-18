// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_with_an_idempotency_key;

public class and_the_key_storage_is_unavailable : given.an_event_sequence_with_idempotency_keys
{
    async Task Establish()
    {
        await Protect();
        _keys.TryGetFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>(), Arg.Any<EncryptionKeyRevision?>()).Throws(new Exception("key store unavailable"));
    }
    async Task Because() => _result = await AppendKeyed();
    [Fact] void should_report_an_append_error() => _result.Result.Errors.Single().Value.ShouldEqual("key store unavailable");
    [Fact] void should_not_claim_a_key_conflict() => _result.Conflicts.ShouldBeEmpty();
    [Fact] async Task should_not_write() => await ShouldNotWrite();
}
