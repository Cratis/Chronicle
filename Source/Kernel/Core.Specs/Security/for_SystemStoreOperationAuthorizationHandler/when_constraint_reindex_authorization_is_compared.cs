// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Events.Constraints.Operator;

namespace Cratis.Chronicle.Security.for_SystemStoreOperationAuthorizationHandler;

public class when_constraint_reindex_authorization_is_compared : given.a_query_operation_policy
{
    readonly List<bool> _parity = [];

    async Task Because()
    {
        foreach (var administrator in new[] { false, true })
        {
            _authorization.IsAuthorized(Arg.Any<ClaimsPrincipal>()).Returns(administrator);
            await CompareModes();
        }
    }

    async Task CompareModes()
    {
        foreach (var enforce in new[] { false, true })
        {
            _enforce = enforce;
            foreach (var destructive in new[] { false, true })
            {
                _enforceDestructive = destructive;
                await Compare("System");
                await Compare("sYsTeM+namespace");
                await Compare("application");
            }
        }
    }

    async Task Compare(string store)
    {
        Query(typeof(ConstraintReindexCapability).GetMethod(nameof(ConstraintReindexCapability.CanReindexConstraints))!, new() { ["eventStore"] = store });
        await AuthorizeOperation();
        var probeAllowed = _context.HasSucceeded;
        var command = new ReindexConstraints(store, "default", "event-log", []);
        _operation = new CommandContext(CorrelationId.New(), command.GetType(), command, [], new CommandContextValues(), ServiceProvider: _services);
        await AuthorizeOperation();
        _parity.Add(probeAllowed == _context.HasSucceeded);
    }

    [Fact] void should_make_the_same_policy_decision_without_executing_either_operation() => _parity.ShouldEachConformTo(_ => _);
    [Fact] void should_cover_both_caller_kinds_every_mode_and_each_store_spelling() => _parity.Count.ShouldEqual(24);
}
