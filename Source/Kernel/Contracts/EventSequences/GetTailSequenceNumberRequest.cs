// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Cratis.Chronicle.Contracts.EventSequences;

/// <summary>
/// Represents the request for getting the tail sequence number.
/// </summary>
[ProtoContract]
public class GetTailSequenceNumberRequest : IEventSequenceRequest
{
    /// <inheritdoc/>
    [ProtoMember(1)]
    public string EventStoreName { get; set; }

    /// <inheritdoc/>
    [ProtoMember(2)]
    public string Namespace { get; set; }

    /// <inheritdoc/>
    [ProtoMember(3)]
    public string EventSequenceId { get; set; }
}
