// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.AspNetCore.Transactions;

internal sealed class UnitOfWorkEndpointHarness : IEndpointConventionBuilder
{
    readonly RouteEndpointBuilder _builder = new(_ => Task.CompletedTask, RoutePatternFactory.Parse("/"), 0);

    internal UnitOfWorkHarness Request { get; } = new();
    internal object Original { get; } = new();
    internal object? Result { get; set; }
    internal Exception? Failure { get; set; }
    internal bool CompletedInHandler { get; set; }
    internal bool CompletedBeforeResult { get; set; }
    internal bool ClaimFirst { get; set; }
    internal bool CompleteFirst { get; set; }
    internal bool RemoveOwnership { get; set; }
    internal IUnitOfWorkCompletionClaim? ExistingClaim { get; set; }

    public void Add(Action<EndpointBuilder> convention) => convention(_builder);

    internal Task Run() => Request.Run(async http =>
    {
        Request.AddEvent();
        if (RemoveOwnership) http.Features.Set<IUnitOfWorkCompletionOwnershipFeature>(null);
        if (CompleteFirst) await Request.Unit.Commit();
        if (ClaimFirst) ExistingClaim = http.Features.Get<IUnitOfWorkCompletionOwnershipFeature>()!.Claim();
        this.WithUnitOfWork();
        var factoryContext = new EndpointFilterFactoryContext
        {
            ApplicationServices = new ServiceCollection().BuildServiceProvider(),
            MethodInfo = typeof(object).GetMethod(nameof(ToString))!
        };
        var pipeline = _builder.FilterFactories.Single()(factoryContext, _ =>
        {
            CompletedInHandler = Request.Unit.IsCompleted;
            if (Failure is not null) throw Failure;
            return ValueTask.FromResult<object?>(Original);
        });
        Result = await pipeline(EndpointFilterInvocationContext.Create(http));
        CompletedBeforeResult = Request.Unit.IsCompleted;
        if (ExistingClaim is not null) await ExistingClaim.Rollback();
    });
}
