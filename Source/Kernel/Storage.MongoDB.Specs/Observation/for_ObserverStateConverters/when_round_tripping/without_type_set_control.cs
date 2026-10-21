// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverStateConverters.when_round_tripping;

public class without_type_set_control : Specification
{
    BsonDocument _document;
    KernelObserverState _ordinary;
    KernelObserverState _joined;

    void Because()
    {
        _document = new KernelObserverState { Identifier = "legacy-observer" }.ToMongoDB().ToBsonDocument();
        _ordinary = BsonSerializer.Deserialize<ObserverState>(_document).ToKernel();
        _joined = BsonSerializer.Deserialize<ObserverStateWithFailedPartitions>(_document).ToKernel();
    }

    [Fact] void should_omit_the_new_field() => _document.Contains("typeSetControl").ShouldBeFalse();
    [Fact] void should_leave_the_legacy_observer_uninitialized() => _ordinary.TypeSetControl.ShouldBeNull();
    [Fact] void should_leave_the_joined_legacy_observer_uninitialized() => _joined.TypeSetControl.ShouldBeNull();
}
