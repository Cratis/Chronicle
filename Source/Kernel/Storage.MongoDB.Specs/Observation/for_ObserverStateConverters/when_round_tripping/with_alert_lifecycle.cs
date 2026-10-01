// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverStateConverters.when_round_tripping;

public class with_alert_lifecycle : Specification
{
    KernelObserverState _original;
    KernelObserverState _ordinary;
    KernelObserverState _joined;

    void Establish() => _original = new()
    {
        Identifier = "observer",
        AlertLifecycleId = Guid.NewGuid(),
        AlertRevision = 42,
        AlertDisposition = AlertDisposition.Removing,
        QuarantineEpisodeId = Guid.NewGuid()
    };

    void Because()
    {
        var bson = _original.ToMongoDB().ToBson();
        _ordinary = BsonSerializer.Deserialize<ObserverState>(bson).ToKernel();
        _joined = BsonSerializer.Deserialize<ObserverStateWithFailedPartitions>(bson).ToKernel();
    }

    [Fact] void should_round_trip_the_lifecycle() => _ordinary.AlertLifecycleId.ShouldEqual(_original.AlertLifecycleId);
    [Fact] void should_round_trip_the_revision() => _ordinary.AlertRevision.ShouldEqual(42);
    [Fact] void should_round_trip_the_disposition() => _ordinary.AlertDisposition.ShouldEqual(AlertDisposition.Removing);
    [Fact] void should_round_trip_the_quarantine() => _ordinary.QuarantineEpisodeId.ShouldEqual(_original.QuarantineEpisodeId);
    [Fact] void should_preserve_the_joined_lifecycle() => _joined.AlertLifecycleId.ShouldEqual(_original.AlertLifecycleId);
    [Fact] void should_preserve_the_joined_revision() => _joined.AlertRevision.ShouldEqual(42);
    [Fact] void should_preserve_the_joined_disposition() => _joined.AlertDisposition.ShouldEqual(AlertDisposition.Removing);
    [Fact] void should_preserve_the_joined_quarantine() => _joined.QuarantineEpisodeId.ShouldEqual(_original.QuarantineEpisodeId);
}
