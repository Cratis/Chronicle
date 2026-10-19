// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Transactions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.AspNetCore.Transactions.for_UnitOfWorkCompletionClaim;

public class when_claiming_twice : Specification
{
    readonly UnitOfWorkHarness _harness = new();
    Exception _error;
    bool _tryClaim;
    bool _wasClaimed;

    async Task Because() => await _harness.Run(context =>
    {
        var feature = context.Features.Get<IUnitOfWorkCompletionOwnershipFeature>()!;
        feature.Claim();
        _wasClaimed = feature.IsClaimed;
        _error = Record.Exception(() => feature.Claim());
        _tryClaim = feature.TryClaim(out _);
        return Task.CompletedTask;
    });

    [Fact] void should_be_claimed() => _wasClaimed.ShouldBeTrue();
    [Fact] void should_throw_already_claimed() => _error.ShouldBeOfExactType<UnitOfWorkCompletionAlreadyClaimed>();
    [Fact] void should_not_allow_a_second_try_claim() => _tryClaim.ShouldBeFalse();
}
