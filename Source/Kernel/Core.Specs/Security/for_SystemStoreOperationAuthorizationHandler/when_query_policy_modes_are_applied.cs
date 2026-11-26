// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints.Operator;

namespace Cratis.Chronicle.Security.for_SystemStoreOperationAuthorizationHandler;

public class when_query_policy_modes_are_applied : given.a_query_operation_policy
{
    readonly List<bool> _ordinarySystem = [];
    readonly List<bool> _destructiveSystem = [];
    readonly List<bool> _ordinaryStore = [];
    readonly List<bool> _kernel = [];
    readonly List<bool> _probe = [];

    async Task Because()
    {
        foreach (var enforce in new[] { false, true })
        {
            foreach (var destructive in new[] { false, true })
            {
                await CheckMode(enforce, destructive);
            }
        }
    }

    async Task CheckMode(bool enforce, bool destructive)
    {
        _enforce = enforce;
        _enforceDestructive = destructive;
        _ordinarySystem.Add(await Allows(nameof(given.operation_queries.Ordinary), new() { ["eventStore"] = "System" }) == !enforce);
        _destructiveSystem.Add(await Allows(nameof(given.operation_queries.Destructive), new() { ["eventStore"] = "System" }) == !(enforce || destructive));
        _ordinaryStore.Add(await Allows(nameof(given.operation_queries.Ordinary), new() { ["eventStore"] = "application" }));
        _ordinaryStore.Add(await Allows(nameof(given.operation_queries.Destructive), new() { ["eventStore"] = "application" }));
        _kernel.Add(await Allows(nameof(given.operation_queries.Kernel)) == !enforce);
        Query(typeof(ConstraintReindexCapability).GetMethod(nameof(ConstraintReindexCapability.CanReindexConstraints))!, new() { ["eventStore"] = "System" });
        await AuthorizeOperation();
        _probe.Add(_context.HasSucceeded == !(enforce || destructive));
    }

    [Fact] void should_apply_the_broad_switch_to_ordinary_system_queries() => _ordinarySystem.ShouldEachConformTo(_ => _);
    [Fact] void should_apply_either_switch_to_destructive_system_queries() => _destructiveSystem.ShouldEachConformTo(_ => _);
    [Fact] void should_leave_ordinary_stores_unenrolled_in_every_mode() => _ordinaryStore.ShouldEachConformTo(_ => _);
    [Fact] void should_treat_unscoped_queries_as_kernel_operations() => _kernel.ShouldEachConformTo(_ => _);
    [Fact] void should_classify_the_real_constraint_probe_like_its_command() => _probe.ShouldEachConformTo(_ => _);
}
