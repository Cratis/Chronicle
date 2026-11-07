// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Services;
using Microsoft.Extensions.Options;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Server.for_GrpcCallerResolution.given;

/// <summary>
/// Answers with what the kernel's gRPC guards resolve from the call: the caller and its decision.
/// </summary>
/// <param name="options">The <see cref="ChronicleOptions"/>.</param>
public class GuardProbe(IOptions<ChronicleOptions> options) : IGuardProbe
{
    public async Task<ProbeReply> Decide(ProbeRequest request, CallContext context = default) => new()
    {
        Owner = ConnectionCallers.From(context, options.Value).Owner.Subject,
        Permitted = await StoreCapabilityGuard.Permits(context, request.Store, null, StoreAccess.Append)
    };

    public IObservable<ProbeReply> Watch(ProbeRequest request, CallContext context = default) =>
        StoreCapabilityGuard.EndWhenRevoked(
            Observable.Return(new ProbeReply { Permitted = true }).Concat(Observable.Never<ProbeReply>()),
            context,
            request.Store,
            null,
            StoreAccess.Read);
}
