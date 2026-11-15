// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Security.for_SystemStoreOperationAuthorizationHandler;

public class when_destructive_operations_are_enforced_by_store : given.an_operation_policy
{
    static object Operation(string kind, string store) => kind switch
    {
        nameof(FastForward) => new FastForward(store, "default", "observer", string.Empty),
        nameof(ReplayObserver) => new ReplayObserver(store, "default", "observer", string.Empty),
        _ => new DeleteJob(store, "default", JobId.New())
    };

    public static TheoryData<string> Kinds() => [nameof(FastForward), nameof(ReplayObserver), nameof(DeleteJob)];

    void Establish() => _enforceDestructive = true;

    [Theory]
    [MemberData(nameof(Kinds))]
    public async Task should_refuse_a_non_administrator_against_the_system_store(string kind)
    {
        _operation = Operation(kind, "System");
        await AuthorizeOperation();
        _context.HasSucceeded.ShouldBeFalse();
        _logger.Entries.Single().Message.ShouldContain($"{kind} is refused");
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public async Task should_leave_an_ordinary_store_to_its_own_rules(string kind)
    {
        _operation = Operation(kind, "an-ordinary-store");
        await AuthorizeOperation();
        _context.HasSucceeded.ShouldBeTrue();
        _logger.Entries.ShouldBeEmpty();
    }
}
