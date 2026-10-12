// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Sequencing.for_SequencingKeyIdentity.when_restoring_stored_columns;

public class and_the_slice_was_cloned_out_of_range : Specification
{
    SequencingKeyIdentity _identity;
    SequencingKeySlice _slice;
    Exception? _error;

    void Establish()
    {
        _identity = SequencingKeyIdentity.For(given.golden_keys.Key(0));
        _slice = new SequencingKeySlice(1) with { Value = 2000 };
    }

    void Because() => _error = Catch.Exception(() => SequencingKeyIdentity.FromStored(_identity.Kind, _identity.Hash, _identity.Key, _slice));

    [Fact] void should_reject_the_cloned_slice() => _error.ShouldBeOfExactType<SequencingKeyIdentityMismatch>();
}
