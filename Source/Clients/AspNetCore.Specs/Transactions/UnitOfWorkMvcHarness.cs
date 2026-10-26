// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.AspNetCore.Transactions;

internal sealed class UnitOfWorkMvcHarness
{
    readonly IAsyncActionFilter _action;
    readonly UnitOfWorkResultFilter _filter;

    internal UnitOfWorkHarness Request { get; } = new();
    internal ChronicleAspNetCoreOptions Options { get; } = new();
    internal IActionResult Original { get; } = new OkObjectResult("original");
    internal IActionResult? Result { get; set; }
    internal bool CompletedBeforeResult { get; set; }
    internal bool Canceled { get; set; }
    internal Exception? Exception { get; set; }
    internal bool ActionRan { get; set; } = true;
    internal bool ClaimFirst { get; set; }
    internal bool CompleteFirst { get; set; }
    internal bool RemoveOwnership { get; set; }
    internal bool AddEvents { get; set; } = true;
    internal IUnitOfWorkCompletionClaim? ExistingClaim { get; set; }

    internal UnitOfWorkMvcHarness()
    {
        Options.UnitOfWork.ReportFailuresBeforeResponseStarts = true;
        using var services = new ServiceCollection().AddUnitOfWork()
            .AddSingleton<IOptions<ChronicleAspNetCoreOptions>>(Microsoft.Extensions.Options.Options.Create(Options))
            .BuildServiceProvider();
        var filters = services.GetRequiredService<IOptions<MvcOptions>>().Value.Filters;
        _action = filters.OfType<IAsyncActionFilter>().Single();
        _filter = filters.OfType<UnitOfWorkResultFilter>().Single();
    }

    internal Task Run() => Request.Run(Invoke);

    async Task Invoke(HttpContext http)
    {
        if (AddEvents) Request.AddEvent();
        if (RemoveOwnership) http.Features.Set<IUnitOfWorkCompletionOwnershipFeature>(null);
        if (ClaimFirst) ExistingClaim = http.Features.Get<IUnitOfWorkCompletionOwnershipFeature>()!.Claim();
        if (CompleteFirst) await Request.Unit.Commit();
        var context = new ActionContext(http, new RouteData(), new ActionDescriptor());
        var controller = new object();
        if (ActionRan) await ExecuteAction(context, controller);
        await ExecuteResult(context, controller);
        if (ExistingClaim is not null) await ExistingClaim.Rollback();
    }

    Task ExecuteAction(ActionContext context, object controller) => _action.OnActionExecutionAsync(
        new ActionExecutingContext(context, [], new Dictionary<string, object?>(), controller),
        () => Task.FromResult(new ActionExecutedContext(context, [], controller)
        {
            Result = Original,
            Canceled = Canceled,
            Exception = Exception,
            ExceptionHandled = Exception is not null
        }));

    async Task ExecuteResult(ActionContext context, object controller)
    {
        var executing = new ResultExecutingContext(context, [], Original, controller);
        await _filter.OnResultExecutionAsync(executing, () =>
        {
            Result = executing.Result;
            CompletedBeforeResult = Request.Unit.IsCompleted;
            return Task.FromResult(new ResultExecutedContext(context, [], executing.Result, controller));
        });
    }
}
