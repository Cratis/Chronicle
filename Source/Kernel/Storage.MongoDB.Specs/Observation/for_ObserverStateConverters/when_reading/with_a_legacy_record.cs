// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Bson.Serialization;

using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverStateConverters.when_reading;

public class with_a_legacy_record : Specification
{
    KernelObserverState _saved;
    KernelObserverState _joined;

    void Because()
    {
        var document = new KernelObserverState { Identifier = "observer" }.ToMongoDB().ToBsonDocument();
        document.Remove(BsonClassMap.LookupClassMap(typeof(ObserverState)).GetMemberMap(nameof(ObserverState.RunningStateChangedAt)).ElementName);
        _saved = BsonSerializer.Deserialize<ObserverState>(document).ToKernel();
        _joined = BsonSerializer.Deserialize<ObserverStateWithFailedPartitions>(document).ToKernel();
    }

    [Fact] void should_leave_the_timestamp_unknown() => _saved.RunningStateChangedAt.ShouldBeNull();
    [Fact] void should_leave_the_joined_timestamp_unknown() => _joined.RunningStateChangedAt.ShouldBeNull();
}
