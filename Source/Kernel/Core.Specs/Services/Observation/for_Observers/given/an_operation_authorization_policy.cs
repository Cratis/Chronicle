// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Security.for_SystemStoreOperationAuthorizationHandler.given;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Observation.for_Observers.given;

public class an_operation_authorization_policy : all_dependencies
{
    protected IAdministratorAuthorization _administratorAuthorization;
    protected RecordingLogger _logger;
    protected ClaimsPrincipal _principal;
    protected CallContext _callContext;
    ServiceProvider _provider;

    protected void ConfigurePolicy(bool enforce)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(_administratorAuthorization);
        services.AddSingleton<ILogger<SystemStoreOperationAuthorizationHandler>>(_logger);
        services.AddSingleton<IOptions<ChronicleOptions>>(Options.Create(new ChronicleOptions
        {
            Authentication = new Authentication { EnforceAdministratorPolicyForOperations = enforce }
        }));
        services.AddSingleton<IAuthorizationHandler, SystemStoreOperationAuthorizationHandler>();
        services.AddAuthorizationBuilder().AddPolicy(
            WellKnownAuthorizationPolicies.AdministratorOperations,
            policy => policy.AddRequirements(new SystemStoreOperationRequirement()));
        _provider = services.BuildServiceProvider();
        _observers = new Observers(_grainFactory, _storage, _observerRemover, _provider.GetRequiredService<IAuthorizationService>(), _httpContextAccessor);
    }

    void Establish()
    {
        _administratorAuthorization = Substitute.For<IAdministratorAuthorization>();
        _logger = new RecordingLogger();
        _principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("access_token", "do-not-log-this-token")], "Bearer"));
        _httpContextAccessor.HttpContext.Returns(new DefaultHttpContext { User = _principal });
    }

    async Task Destroy()
    {
        if (_provider is not null) await _provider.DisposeAsync();
    }
}
