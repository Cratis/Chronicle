// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Security.for_SystemStoreOperationAuthorizationHandler;

public class when_queries_overlap_on_one_handler : given.a_query_operation_policy
{
    readonly TaskCompletionSource<bool> _privilege = new(TaskCreationOptions.RunContinuationsAsynchronously);
    AuthorizationHandlerContext _system;
    AuthorizationHandlerContext _ordinary;

    void Establish()
    {
        _enforce = true;
        _authorization.IsAuthorized(Arg.Any<ClaimsPrincipal>()).Returns(_privilege.Task);
    }

    async Task Because()
    {
        var options = Options.Create(new ChronicleOptions { Authentication = new Authentication { Enabled = true, EnforceAdministratorPolicyForOperations = false, EnforceDestructiveOperations = true } });
        var decisions = new AuthorizationDecisions(options, NullLogger<AuthorizationDecisions>.Instance);
        var handler = new SystemStoreOperationAuthorizationHandler(decisions, options, _logger);
        Query(nameof(given.operation_queries.Destructive), new() { ["eventStore"] = "System" });
        _system = Context();
        var pending = handler.HandleAsync(_system);
        Query(nameof(given.operation_queries.Ordinary), new() { ["eventStore"] = "application" });
        _ordinary = Context();
        await handler.HandleAsync(_ordinary);
        _privilege.SetResult(false);
        await pending;
    }

    AuthorizationHandlerContext Context() => new([new SystemStoreOperationRequirement()], _principal, _query);

    [Fact] void should_refuse_the_system_operation() => _system.HasSucceeded.ShouldBeFalse();
    [Fact] void should_not_leak_system_scope_into_an_overlapping_ordinary_query() => _ordinary.HasSucceeded.ShouldBeTrue();
    [Fact] void should_keep_the_logged_identity_operation_local() => _logger.Entries[0].Message.ShouldContain(nameof(given.operation_queries.Destructive));
}
