// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
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
/// <param name="replies">The controlled stream replies.</param>
public class GuardProbe(IOptions<ChronicleOptions> options, Subject<ProbeReply> replies) : IGuardProbe
{
    public async Task<ProbeReply> Decide(ProbeRequest request, CallContext context = default) => new()
    {
        Owner = ConnectionCallers.From(context, options.Value).Owner.Subject,
        Permitted = await StoreCapabilityGuard.Permits(context, request.Store, null, StoreAccess.Append)
    };

    public IObservable<ProbeReply> Watch(ProbeRequest request, CallContext context = default) =>
        StoreCapabilityGuard.EndWhenRevoked(
            Observable.Return(new ProbeReply { Permitted = true }).Concat(replies),
            context,
            request.Store,
            null,
            StoreAccess.Read);

    public IObservable<ProbeReply> CompleteWithRecordedFailure(ProbeRequest request, CallContext context = default)
    {
        context.RecordFailure( new global::Grpc.Core.RpcException(new(global::Grpc.Core.StatusCode.PermissionDenied, "Refused")));
        return Observable.Empty<ProbeReply>();
    }
}
