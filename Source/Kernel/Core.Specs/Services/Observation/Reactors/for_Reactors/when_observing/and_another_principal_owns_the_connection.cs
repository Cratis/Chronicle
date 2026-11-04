// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Security.Claims;
using System.Text.Json;
using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Observation.Reactors.Clients;
using Cratis.Chronicle.Storage;
using Cratis.Traces;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Observation.Reactors.for_Reactors.when_observing;

public class and_another_principal_owns_the_connection : Specification
{
    readonly ConnectionOwnership _ownership = new();
    readonly IReactorMediator _mediator = Substitute.For<IReactorMediator>();
    Exception _error;

    void Establish() => _ownership.Claim("connection", new("original"));

    async Task Because()
    {
        var options = Options.Create(new ChronicleOptions());
        var service = new Reactors(Substitute.For<IGrainFactory>(), _mediator, Substitute.For<IStorage>(), new JsonSerializerOptions(), new ActivitySource<Reactors>(), options, NullLogger<Reactors>.Instance, _ownership, new ConnectionOwnerResolver(options));
        var context = Substitute.ForPartsOf<ServerCallContext>();
        context.UserState["__HttpContext"] = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "other")], "test")) };
        var completion = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = service.Observe(Observable.Return(new ReactorMessage(new(new RegisterReactor { ConnectionId = "connection", Reactor = new() { ReactorId = "reactor" } }))), new CallContext(this, context)).Subscribe(_ => { }, completion.SetResult);
        _error = await completion.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_deny_observation() => ((RpcException)_error).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
    [Fact] void should_not_touch_the_mediator() => _mediator.ReceivedCalls().ShouldBeEmpty();
}
