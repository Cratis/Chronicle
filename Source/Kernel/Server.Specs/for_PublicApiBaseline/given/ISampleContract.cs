// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace Cratis.Chronicle.Server.for_PublicApiBaseline.given;

[Service]
internal interface ISampleContract
{
    [Operation]
    Task<EventType> Read(EventType request, CallContext context = default);
}
