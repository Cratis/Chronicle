// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Reactors;
using ProtoBuf;

namespace Cratis.Chronicle.Contracts.for_RegisterReactor.when_serializing;

[ProtoContract]
public class ReleasedRegisterReactor
{
    [ProtoMember(1)]
    public string ConnectionId { get; set; }

    [ProtoMember(2)]
    public string EventStore { get; set; }

    [ProtoMember(3)]
    public string Namespace { get; set; }

    [ProtoMember(4)]
    public ReactorDefinition Reactor { get; set; }
}
