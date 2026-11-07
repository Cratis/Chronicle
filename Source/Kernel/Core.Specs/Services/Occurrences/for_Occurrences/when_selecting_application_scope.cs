// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Cratis.Chronicle.Contracts.Occurrences;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Services.given;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Services.Occurrences.for_Occurrences;

public class when_selecting_application_scope
{
    [Theory]
    [InlineData("tenant", true, false)]
    [InlineData(null, true, true)]
    [InlineData("tenant", false, true)]
    public async Task should_use_the_same_effective_scope_for_arc_and_grpc(string? grantNamespace, bool applicationScope, bool allowed)
    {
        using var caller = new an_entitled_caller(ns: grantNamespace);
        var arguments = new QueryArguments { ["eventStore"] = "store", ["namespace"] = "tenant", ["applicationScope"] = applicationScope };
        var context = new QueryContext("Cratis.Chronicle.Occurrences.OccurrenceSnapshot.GetOccurrence", CorrelationId.New(), Paging.NotPaged, Sorting.None, arguments, ServiceProvider: caller.Http.RequestServices);
        var arc = await new SystemStoreQueryFilter().OnPerform(context);
        (arc is null).ShouldEqual(allowed);
        var service = new Occurrences(Substitute.For<ICommandPipeline>(), Substitute.For<IStorage>(), NullLogger<Occurrences>.Instance);
        var grpc = await service.GetOccurrence(new GetOccurrenceRequest { EventStore = "store", Namespace = "tenant", OccurrenceId = "one", ApplicationScope = applicationScope }, caller.Context);
        grpc.IsAuthorized.ShouldEqual(allowed);
    }
}
