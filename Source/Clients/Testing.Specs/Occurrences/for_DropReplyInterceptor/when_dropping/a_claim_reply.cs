// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Occurrences;
using Grpc.Core;

namespace Cratis.Chronicle.Testing.Occurrences.for_DropReplyInterceptor.when_dropping;

public class a_claim_reply : given.an_occurrence
{
    Exception? _error;
    bool _nothingBeforeExpiry;
    OccurrenceClaimDetails _reclaimed;

    async Task Because()
    {
        _scenario.Replies.DropNext(DroppedReply.ClaimOccurrences);
        _error = await Catch.Exception(() => _wire.ClaimOccurrences(_claim).EnsureSuccess());
        _nothingBeforeExpiry = !(await _wire.ClaimOccurrences(_claim).EnsureSuccess()).Claims.Any();
        _scenario.Time.Advance(TimeSpan.FromSeconds(60));
        _reclaimed = await Claim();
    }

    [Fact] void should_discard_the_claim_reply() => _error.ShouldBeOfExactType<RpcException>();
    [Fact] void should_keep_the_unknown_claim_leased() => _nothingBeforeExpiry.ShouldBeTrue();
    [Fact] void should_reclaim_at_a_new_epoch() => _reclaimed.Epoch.ShouldEqual(2L);
    [Fact] void should_keep_the_occurrence_identity() => _reclaimed.Id.ShouldEqual(_recorded.Id.Value);
}
