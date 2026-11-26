// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Security.for_SystemStoreOperationAuthorizationHandler;

public class when_a_query_has_no_bindable_store_parameter : given.a_query_operation_policy
{
    bool _kernel;
    bool _injected;

    void Establish() => _enforce = true;

    async Task Because()
    {
        _kernel = await Allows(nameof(given.operation_queries.Kernel), new() { ["eventStore"] = "application" });
        var classification = Substitute.For<IServiceProviderIsService>();
        classification.IsService(typeof(IServiceProvider)).Returns(true);
        Query(typeof(given.operation_queries).GetMethod(nameof(given.operation_queries.Injected))!, new QueryArguments { ["eventStore"] = "application" }, classification);
        await AuthorizeOperation();
        _injected = _context.HasSucceeded;
    }

    [Fact] void should_ignore_a_spoofed_store_on_a_kernel_query() => _kernel.ShouldBeFalse();
    [Fact] void should_not_turn_an_injected_dependency_into_a_store() => _injected.ShouldBeFalse();
}
