// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace Cratis.Chronicle.Server.for_GrpcCallerResolution.given;

[Service]
public interface IGuardProbe
{
    [Operation]
    Task<ProbeReply> Decide(ProbeRequest request, CallContext context = default);

    [Operation]
    IObservable<ProbeReply> Watch(ProbeRequest request, CallContext context = default);
}
