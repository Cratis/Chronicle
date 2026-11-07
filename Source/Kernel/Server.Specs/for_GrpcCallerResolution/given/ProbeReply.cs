// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Cratis.Chronicle.Server.for_GrpcCallerResolution.given;

[ProtoContract]
public class ProbeReply
{
    [ProtoMember(1)]
    public string Owner { get; set; } = string.Empty;

    [ProtoMember(2)]
    public bool Permitted { get; set; }
}
