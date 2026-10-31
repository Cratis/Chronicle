// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Identities;
using Microsoft.AspNetCore.DataProtection;

namespace Cratis.Chronicle.Security.for_KernelEventProof.given;

public class a_kernel_event_proof : Specification
{
    protected IKernelEventProof _proof;
    protected EventType _type;
    protected EventSourceId _source;
    protected EventSequenceNumber _predecessor;
    protected string _signed;

    void Establish()
    {
        _proof = new KernelEventProof(new EphemeralDataProtectionProvider());
        _type = typeof(UserGrantedAdministrator).GetEventType();
        _source = "roster";
        _predecessor = EventSequenceNumber.BeforeFirst;
        _signed = _proof.Sign(_type, _source, _predecessor, "body");
    }
}
