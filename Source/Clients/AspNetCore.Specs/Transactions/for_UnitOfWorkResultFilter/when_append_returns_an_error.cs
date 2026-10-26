// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.Chronicle.AspNetCore.Transactions.for_UnitOfWorkResultFilter;

public class when_append_returns_an_error : Specification
{
    readonly UnitOfWorkMvcHarness _harness = new();

    void Establish() => _harness.Request.AppendReturns(AppendManyResult.Failed(_harness.Request.CorrelationId, [new AppendError("append failed")]));
    async Task Because() => await _harness.Run();

    [Fact] void should_return_internal_server_error() => ((ObjectResult)_harness.Result!).StatusCode.ShouldEqual(500);
}
