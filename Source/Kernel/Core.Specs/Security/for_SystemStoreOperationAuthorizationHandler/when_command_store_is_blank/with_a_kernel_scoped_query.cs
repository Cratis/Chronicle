// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Events.Constraints.Operator;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Security.for_SystemStoreOperationAuthorizationHandler.when_command_store_is_blank;

public class with_a_kernel_scoped_query : given.a_query_operation_policy
{
    readonly List<bool> _matchesKernel = [];
    readonly List<bool> _matchesPolicy = [];

    async Task Because()
    {
        foreach (var administrator in new[] { false, true })
        {
            _authorization.IsAuthorized(Arg.Any<ClaimsPrincipal>()).Returns(administrator);
            await CompareModes(administrator);
        }
    }

    async Task CompareModes(bool administrator)
    {
        foreach (var enforce in new[] { false, true })
        {
            _enforce = enforce;
            foreach (var destructive in new[] { false, true })
            {
                _enforceDestructive = destructive;
                foreach (var store in new string?[] { null, "", " \t\r\n" })
                {
                    await Compare(store, administrator || !(enforce || destructive));
                }
            }
        }
    }

    async Task Compare(string? store, bool expected)
    {
        var queryAllowed = await Allows(nameof(given.operation_queries.DestructiveKernel));
        object[] commands =
        [
            new ReplayObserver(store!, "default", "observer", string.Empty),
            new ReindexConstraints(store is null ? null! : new EventStoreName(store), "default", "event-log", [])
        ];
        foreach (var command in commands)
        {
            _operation = new CommandContext(CorrelationId.New(), command.GetType(), command, [], new CommandContextValues(), ServiceProvider: _services);
            await AuthorizeOperation();
            _matchesKernel.Add(_context.HasSucceeded == queryAllowed);
            _matchesPolicy.Add(_context.HasSucceeded == expected);
        }
    }

    [Fact] void should_authorize_blank_command_stores_like_kernel_queries() => _matchesKernel.ShouldEachConformTo(_ => _);
    [Fact] void should_apply_administrator_and_enforcement_policy() => _matchesPolicy.ShouldEachConformTo(_ => _);
    [Fact] void should_cover_null_empty_and_whitespace_for_strings_and_concepts() => _matchesKernel.Count.ShouldEqual(48);
}
