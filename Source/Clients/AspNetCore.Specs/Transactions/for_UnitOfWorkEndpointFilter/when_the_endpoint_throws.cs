// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.AspNetCore.Transactions.for_UnitOfWorkEndpointFilter;

public class when_the_endpoint_throws : Specification
{
    readonly UnitOfWorkEndpointHarness _harness = new() { Failure = new InvalidOperationException("endpoint failed") };
    Exception _exception;

    async Task Because() => _exception = await Catch.Exception(_harness.Run);

    [Fact] void should_rethrow_the_same_exception() => _exception.ShouldEqual(_harness.Failure);
    [Fact] void should_roll_back_the_unit() => _harness.Request.Unit.IsCompleted.ShouldBeTrue();
    [Fact] void should_not_append() => _harness.Request.AppendCalls.ShouldEqual(0);
}
