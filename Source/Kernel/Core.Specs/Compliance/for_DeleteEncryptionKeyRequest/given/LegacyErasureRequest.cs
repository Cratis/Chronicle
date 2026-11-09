// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Cratis.Chronicle.Compliance.for_DeleteEncryptionKeyRequest.given;

[ProtoContract]
public class LegacyErasureRequest
{
    [ProtoMember(1)] public string EventStore { get; set; } = string.Empty;
    [ProtoMember(2)] public string Namespace { get; set; } = string.Empty;
    [ProtoMember(3)] public string Identifier { get; set; } = string.Empty;
}
