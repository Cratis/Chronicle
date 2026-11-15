// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Setup;
using Cratis.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Security.for_AdministratorAuthorizationServiceCollectionExtensions;

/// <summary>
/// A transport such as the gRPC command executor calls the pipeline without a provider. The pipeline then creates its own
/// scope from the root, while the caller is still known from the ambient HTTP context. The administrator check must be
/// resolved from the pipeline's scope, never from the root or the request, so it sees no request state and refuses.
/// </summary>
public class when_the_pipeline_creates_its_own_scope : Specification
{
    [Fact]
    public async Task should_resolve_the_check_from_the_pipeline_scope_and_refuse()
    {
        var observed = new List<CallerState>();
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "caller")], "spec"));
        var services = new ServiceCollection().AddLogging().AddOptions();
        services.Configure<ArcOptions>(_ => { });
        services.AddCratisArcCore();
        services.AddKernelSideCommandExtensionPoints();
        services.AddSingleton<IAuthorizationPolicyRuntime, AspNetAuthorizationPolicyRuntime>();
        var handlers = Substitute.For<ICommandHandlerProviders>();
        handlers.TryGetHandlerFor(Arg.Any<object>(), out Arg.Any<ICommandHandler>()).Returns(call =>
        {
            call[1] = Substitute.For<ICommandHandler>();
            return true;
        });
        services.AddSingleton(handlers);
        services.AddSingleton(Options.Create(new Configuration.ChronicleOptions()));
        services.AddSingleton(Substitute.For<IAdministratorPrivileges>());
        services.AddSingleton(Substitute.For<IGrainFactory>());
        var current = Substitute.For<ICurrentPrincipalAccessor>();
        current.Current.Returns(principal);
        services.AddSingleton(current);
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        services.AddSingleton(httpContextAccessor);
        services.AddScoped<CallerState>();
        services.AddScoped<IAdministratorAuthorization>(scope =>
        {
            var state = scope.GetRequiredService<CallerState>();
            observed.Add(state);
            var check = Substitute.For<IAdministratorAuthorization>();
            check.IsAuthorized(principal).Returns(_ => state.Name is not null);
            return check;
        });
        services.AddChronicleAdministratorAuthorization();
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await using var request = provider.CreateAsyncScope();
        var requestState = request.ServiceProvider.GetRequiredService<CallerState>();
        requestState.Name = "request";
        httpContextAccessor.HttpContext.Returns(new DefaultHttpContext { User = principal, RequestServices = request.ServiceProvider });

        var result = await provider.GetRequiredService<ICommandPipeline>().Execute(new RetryPartition("System", "Default", "observer", "event-log", "partition"));

        result.IsAuthorized.ShouldBeFalse();
        result.IsValid.ShouldBeTrue();
        result.ExceptionMessages.ShouldBeEmpty();
        observed.Count.ShouldBeGreaterThan(0);
        observed.Contains(requestState).ShouldBeFalse();
        observed.TrueForAll(state => state.Disposed && state.Name is null).ShouldBeTrue();
    }

    [IgnoreConvention]
    public sealed class CallerState : IDisposable
    {
        public string? Name { get; set; }
        public bool Disposed { get; private set; }
        public void Dispose() => Disposed = true;
    }
}
