// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Cratis.Chronicle.Benchmarks.Kernel;

/// <summary>
/// The message the client stream benchmark's call sends.
/// </summary>
[ProtoContract]
public class ClientStreamsProbeMessage
{
    /// <summary>
    /// Gets or sets a value.
    /// </summary>
    [ProtoMember(1)]
    public int Value { get; set; }
}
