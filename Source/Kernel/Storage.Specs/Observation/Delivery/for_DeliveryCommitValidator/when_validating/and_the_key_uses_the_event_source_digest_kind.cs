// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.Sequencing;

namespace Cratis.Chronicle.Storage.Observation.Delivery.for_DeliveryCommitValidator.when_validating;

public class and_the_key_uses_the_event_source_digest_kind : given.a_valid_commit
{
    Exception _exception;

    void Establish()
    {
        var key = SequencingKeyIdentity.For(new EventSourceIdSequencingKey(new EventSourceId(new string('a', 8192))));
        _commit = Create(_root, [ParkedKeyOperation.Insert(_parked with { Key = key })], []);
    }

    void Because() => _exception = Catch.Exception(() => DeliveryCommitValidator.Validate(_commit));

    [Fact] void should_accept_the_third_key_kind() => _exception.ShouldBeNull();
    [Fact] void should_keep_the_digest_kind() => _commit.ParkedKeys[0].Row.Key.Kind.ShouldEqual(SequencingKeyKind.EventSourceIdDigest);
}
