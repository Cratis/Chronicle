// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationDigestKey;

public class when_deriving_from_a_short_secret : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => EventSequenceMutationDigestKey.DeriveFrom("event-store", new byte[31]));

    [Fact] void should_refuse_it() => _error.ShouldBeOfExactType<ArgumentException>();
}
