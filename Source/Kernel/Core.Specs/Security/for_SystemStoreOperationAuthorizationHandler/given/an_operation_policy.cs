// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Security.for_SystemStoreOperationAuthorizationHandler.given;

public class an_operation_policy : Specification
{
    protected const string Secret = "must-not-log-token-or-secret";
    protected IAdministratorAuthorization _authorization;
    protected ClaimsPrincipal _principal;
    protected object _operation;
    protected RecordingLogger _logger;
    protected AuthorizationHandlerContext _context;
    protected bool _enforce;
    protected bool _authenticationEnabled = true;
    protected IAuthorizationDecisions? _decisions;

    protected async Task AuthorizeOperation()
    {
        _context = new AuthorizationHandlerContext([new SystemStoreOperationRequirement()], _principal, _operation);
        var options = Options.Create(new ChronicleOptions { Authentication = new Authentication { Enabled = _authenticationEnabled, EnforceAdministratorPolicyForOperations = _enforce } });
        await new SystemStoreOperationAuthorizationHandler(_decisions ?? new AuthorizationDecisions(_authorization, options, NullLogger<AuthorizationDecisions>.Instance), options, _logger).HandleAsync(_context);
    }

    void Establish()
    {
        _authorization = Substitute.For<IAdministratorAuthorization>();
        _principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("access_token", Secret), new Claim("client_secret", Secret)], "Bearer"));
        _operation = new ReplayObserver("System", "default", Secret, string.Empty);
        _logger = new RecordingLogger();
    }
}
