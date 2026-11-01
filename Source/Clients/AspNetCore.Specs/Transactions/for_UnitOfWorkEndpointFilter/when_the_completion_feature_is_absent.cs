// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.AspNetCore.Transactions.for_UnitOfWorkEndpointFilter;

public class when_the_completion_feature_is_absent : Specification
{
    readonly UnitOfWorkEndpointHarness _harness = new() { RemoveOwnership = true };

    async Task Because() => await _harness.Run();

    [Fact] void should_preserve_the_result() => _harness.Result.ShouldEqual(_harness.Original);
    [Fact] void should_not_complete_before_returning_the_result() => _harness.CompletedBeforeResult.ShouldBeFalse();
}
