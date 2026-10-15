// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Sequencing.for_SequencingKeyIdentity.when_restoring_stored_columns;

public class and_the_raw_slice_is_out_of_range : Specification
{
    SequencingKeyIdentity _identity;
    Exception? _error;

    void Establish() => _identity = SequencingKeyIdentity.For(given.golden_keys.Key(0));
    void Because() => _error = Catch.Exception(() => SequencingKeyIdentity.FromStored(_identity.Kind, _identity.Hash.Span, _identity.Key.Span, 1024));

    [Fact] void should_reject_the_inconsistent_identity() => _error.ShouldBeOfExactType<SequencingKeyIdentityMismatch>();
}
