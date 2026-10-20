// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Events.Constraints;
using NSubstitute.Extensions;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.when_folding;

public class and_a_claim_is_refused_by_a_holder_the_fold_changes : given.a_replay
{
    int _attempts;

    async Task Establish()
    {
        await _claims.Save("holder", _definition, 0, Hash("v"));
        Event(1, "claimant", "Claimed", "v");
        Event(2, "holder", "Claimed", "w");
        _unique.Configure().Save("claimant", _definition, 1, Hash("v"), string.Empty, Arg.Any<Subject?>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                _attempts++;
                if (_attempts == 1) throw new DuplicateUniqueConstraintValue(_definition.Name, "claimant");
                return _claims.Save("claimant", _definition, 1, Hash("v"));
            });
        Start();
    }

    async Task Because() => _result = await _replay.Continue();

    [Fact] void should_retry_once_after_every_release() => _attempts.ShouldEqual(2);
    [Fact] async Task should_confirm_the_retried_claim() => (await Values("claimant")).ShouldContainOnly(Hash("v").Value);
    [Fact] void should_not_count_the_temporary_refusal_as_settled() => _result.Settled.ShouldEqual(0);
}
