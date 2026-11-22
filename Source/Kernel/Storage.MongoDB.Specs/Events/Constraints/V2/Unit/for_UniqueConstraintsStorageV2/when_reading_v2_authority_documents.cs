// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.V2.Unit.for_UniqueConstraintsStorageV2;

public class when_reading_v2_authority_documents
{
    [Fact]
    public void should_round_trip_live_and_run_proofs_without_exposing_the_nonce()
    {
        var identity = V2Documents.Identity("sequence", "constraint");
        var live = new UniqueConstraintWriteAuthority(new(Guid.NewGuid()), UniqueConstraintWritePhase.Live, new(long.MaxValue));
        var run = live with { Phase = UniqueConstraintWritePhase.Run(new(Guid.NewGuid())) };
        foreach (var proof in new[] { live, run })
        {
            var document = V2Documents.Proof(identity, proof);
            document.Add("Fence", Guid.NewGuid().ToString("N"));
            V2Documents.Authority(document, identity).ShouldEqual(proof);
        }
    }

    [Theory]
    [InlineData("Sequence")]
    [InlineData("Constraint")]
    [InlineData("Kind")]
    [InlineData("Version")]
    [InlineData("Epoch")]
    [InlineData("Run")]
    [InlineData("Revision")]
    [InlineData("Fence")]
    public void should_fail_closed_for_incomplete_or_wrong_ownership_and_proofs(string field)
    {
        var identity = V2Documents.Identity("sequence", "constraint");
        var authority = new UniqueConstraintWriteAuthority(new(Guid.NewGuid()), UniqueConstraintWritePhase.Live, new(0));
        var document = V2Documents.Proof(identity, authority);
        document.Add("Fence", Guid.NewGuid().ToString("N"));
        document[field] = "damaged";
        Assert.Throws<UniqueConstraintV2SchemaMismatch>(() => V2Documents.Authority(document, identity));
    }
}
