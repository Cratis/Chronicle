// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_SinkCollectionGuards;

public class when_checking_the_promoting_collection : Specification
{
    static readonly SinkCollectionNames _names = SinkCollectionNames.For("things");
    static readonly BsonValue _claimed = new BsonBinaryData(Guid.NewGuid(), GuidRepresentation.Standard);

    [Fact] void should_allow_promoting_the_claimed_collection() => Catch.Exception(() => SinkCollectionGuards.ThrowIfPromotingCollectionChanged(_names, _claimed, new BsonBinaryData(((BsonBinaryData)_claimed).Bytes, BsonBinarySubType.UuidStandard))).ShouldBeNull();
    [Fact] void should_refuse_promoting_another_collection() => Catch.Exception(() => SinkCollectionGuards.ThrowIfPromotingCollectionChanged(_names, _claimed, new BsonBinaryData(Guid.NewGuid(), GuidRepresentation.Standard))).ShouldBeOfExactType<SinkCollectionOperationRefused>();
    [Fact] void should_refuse_when_the_promoting_collection_is_gone() => Catch.Exception(() => SinkCollectionGuards.ThrowIfPromotingCollectionChanged(_names, _claimed, null)).ShouldBeOfExactType<SinkCollectionOperationRefused>();
    [Fact] void should_refuse_when_the_claim_is_unknown() => Catch.Exception(() => SinkCollectionGuards.ThrowIfPromotingCollectionChanged(_names, null, _claimed)).ShouldBeOfExactType<SinkCollectionOperationRefused>();
}
