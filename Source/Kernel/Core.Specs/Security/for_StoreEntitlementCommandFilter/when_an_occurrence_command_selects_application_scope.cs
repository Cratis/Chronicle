// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Occurrences;
using Cratis.Chronicle.Services.given;

namespace Cratis.Chronicle.Security.for_StoreEntitlementCommandFilter;

public class when_an_occurrence_command_selects_application_scope
{
    [Theory]
    [InlineData("tenant", true, false, false)]
    [InlineData(null, true, true, false)]
    [InlineData("tenant", false, true, false)]
    [InlineData("tenant", true, false, true)]
    [InlineData(null, true, true, true)]
    [InlineData("tenant", false, true, true)]
    public async Task should_authorize_the_storage_scope(string? grantNamespace, bool applicationScope, bool allowed, bool write)
    {
        using var caller = new an_entitled_caller(ns: grantNamespace);
        object command = write
            ? new RecordOccurrence("store", "tenant", "trigger", "id", "{}", ApplicationScope: applicationScope)
            : new ClaimOccurrences("store", "tenant", "trigger", "worker", ApplicationScope: applicationScope);
        var context = new CommandContext(CorrelationId.New(), command.GetType(), command, [], new CommandContextValues(), ServiceProvider: caller.Http.RequestServices);
        var result = await new StoreEntitlementCommandFilter().OnExecution(context);
        (result is null).ShouldEqual(allowed);
    }
}
