// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.EventSequences.Mutations.for_EventSequenceMutationIdentity;

public class when_creating_unsupported_identities : Specification
{
    EventSequenceMutationIdentityCreationResult _missing;
    EventSequenceMutationIdentityCreationResult[] _illFormed;
    EventSequenceMutationIdentityCreationResult[] _containingNul;
    EventSequenceMutationIdentityCreationResult[] _tooLong;

    void Because()
    {
        _missing = EventSequenceMutationIdentity.TryCreate(null);
        _illFormed =
        [
            EventSequenceMutationIdentity.TryCreate("\ud800"),
            EventSequenceMutationIdentity.TryCreate("\udc00"),
            EventSequenceMutationIdentity.TryCreate("\udc00\ud800"),
            EventSequenceMutationIdentity.TryCreate("prefix\ud800suffix")
        ];
        _containingNul =
        [
            EventSequenceMutationIdentity.TryCreate("\0"),
            EventSequenceMutationIdentity.TryCreate("before\0after")
        ];
        _tooLong =
        [
            EventSequenceMutationIdentity.TryCreate(new string('a', 201)),
            EventSequenceMutationIdentity.TryCreate(new string('\u0800', 201))
        ];
    }

    [Fact] void should_report_a_missing_value() => _missing.Reason.ShouldEqual(UnsupportedEventSequenceIdReason.MissingValue);
    [Fact] void should_reject_every_ill_formed_utf16_shape() => _illFormed.All(_ => !_.IsSuccess && _.Reason == UnsupportedEventSequenceIdReason.IllFormedUtf16).ShouldBeTrue();
    [Fact] void should_reject_nul_anywhere() => _containingNul.All(_ => !_.IsSuccess && _.Reason == UnsupportedEventSequenceIdReason.ContainsNul).ShouldBeTrue();
    [Fact] void should_reject_more_than_200_utf16_code_units() => _tooLong.All(_ => !_.IsSuccess && _.Reason == UnsupportedEventSequenceIdReason.TooLong).ShouldBeTrue();
    [Fact] void should_not_return_an_identity_for_failure() => new[] { _missing }.Concat(_illFormed).Concat(_containingNul).Concat(_tooLong).All(_ => _.Identity is null).ShouldBeTrue();
}
