// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Microsoft.AspNetCore.DataProtection;

namespace Cratis.Chronicle.Security.for_KernelEventProof;

public class when_verifying_kernel_proof : given.a_kernel_event_proof
{
    bool _roundTrip;
    bool[] _mismatches;

    void Because()
    {
        _roundTrip = _proof.Verify(_type, _source, _predecessor, "body", _signed);
        _mismatches =
        [
            _proof.Verify(typeof(UserRevokedAdministrator).GetEventType(), _source, _predecessor, "body", _signed),
            _proof.Verify(_type, "other", _predecessor, "body", _signed),
            _proof.Verify(_type, _source, EventSequenceNumber.First, "body", _signed),
            _proof.Verify(_type, _source, _predecessor, "other", _signed),
            _proof.Verify(_type, _source, _predecessor, "body", "garbage"),
            _proof.Verify(_type, _source, _predecessor, "body", null),
            _proof.Verify(_type, _source, _predecessor, "body", " "),
            new KernelEventProof(new EphemeralDataProtectionProvider()).Verify(_type, _source, _predecessor, "body", _signed)
        ];
    }

    [Fact] void should_accept_the_original_payload() => _roundTrip.ShouldBeTrue();
    [Fact] void should_reject_every_mismatch() => _mismatches.ShouldEachConformTo(value => !value);
}
