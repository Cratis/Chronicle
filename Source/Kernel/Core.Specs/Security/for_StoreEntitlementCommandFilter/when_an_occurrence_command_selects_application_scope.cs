// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Occurrences;
using Cratis.Chronicle.Services.given;

namespace Cratis.Chronicle.Security.for_StoreEntitlementCommandFilter;

public class when_an_occurrence_command_selects_application_scope
{
    [Theory]
    [InlineData("tenant", true, false)]
    [InlineData(null, true, true)]
    [InlineData("tenant", false, true)]
    public async Task should_authorize_the_storage_scope(string? grantNamespace, bool applicationScope, bool allowed)
    {
        using var caller = new an_entitled_caller(ns: grantNamespace);
        var command = new ClaimOccurrences("store", "tenant", "trigger", "worker", ApplicationScope: applicationScope);
        var context = new CommandContext(CorrelationId.New(), command.GetType(), command, [], new CommandContextValues(), ServiceProvider: caller.Http.RequestServices);
        var result = await new StoreEntitlementCommandFilter().OnExecution(context);
        (result is null).ShouldEqual(allowed);
    }
}
