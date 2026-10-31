// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Sequencing.for_SequencingKeyIdentity.when_restoring_stored_columns;

public class and_they_are_consistent : Specification
{
    SequencingKeyIdentity _original;
    SequencingKeyIdentity _restored;

    void Establish() => _original = SequencingKeyIdentity.For(given.golden_keys.Key(11));
    void Because() => _restored = SequencingKeyIdentity.FromStored(_original.Kind, _original.Hash.Span, _original.Key.Span, _original.Slice.Value);

    [Fact] void should_restore_the_identity() => _restored.ShouldEqual(_original);
}
