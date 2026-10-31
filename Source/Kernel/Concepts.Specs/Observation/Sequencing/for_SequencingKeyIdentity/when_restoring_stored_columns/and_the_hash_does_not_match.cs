// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Sequencing.for_SequencingKeyIdentity.when_restoring_stored_columns;

public class and_the_hash_does_not_match : Specification
{
    SequencingKeyIdentity _identity;
    Exception? _error;

    void Establish() => _identity = SequencingKeyIdentity.For(given.golden_keys.Key(0));
    void Because() => _error = Catch.Exception(() => SequencingKeyIdentity.FromStored(_identity.Kind, SequencingKeyHash.FromStored(new byte[32]), _identity.Key, _identity.Slice));

    [Fact] void should_reject_the_inconsistent_identity() => _error.ShouldBeOfExactType<SequencingKeyIdentityMismatch>();
}
