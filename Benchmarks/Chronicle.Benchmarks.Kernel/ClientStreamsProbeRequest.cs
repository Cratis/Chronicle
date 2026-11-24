// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Cratis.Chronicle.Benchmarks.Kernel;

/// <summary>
/// The empty request of the client stream benchmark's call.
/// </summary>
[ProtoContract]
public record ClientStreamsProbeRequest;
