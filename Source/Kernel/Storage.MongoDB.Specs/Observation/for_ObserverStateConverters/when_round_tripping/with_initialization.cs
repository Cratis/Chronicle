// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverStateConverters.when_round_tripping;

public class with_initialization : Specification
{
    KernelObserverState _legacy;
    KernelObserverState _ordinary;
    KernelObserverState _joined;
    ObserverInitializationState[] _states;
    BsonDocument _legacyDocument;

    void Because()
    {
        _legacyDocument = new KernelObserverState { Identifier = "legacy" }.ToMongoDB().ToBsonDocument();
        _legacy = BsonSerializer.Deserialize<ObserverState>(_legacyDocument).ToKernel();
        var restored = new List<ObserverInitializationState>();
        foreach (var state in Enum.GetValues<ObserverInitializationState>())
        {
            var document = new KernelObserverState { Identifier = "observer", Initialization = new(state, ["unfinished job", "unsettled handover"]), RunningState = ObserverRunningState.Initializing }.ToMongoDB().ToBson();
            _ordinary = BsonSerializer.Deserialize<ObserverState>(document).ToKernel();
            _joined = BsonSerializer.Deserialize<ObserverStateWithFailedPartitions>(document).ToKernel();
            restored.Add(_ordinary.Initialization.State);
        }

        _states = restored.ToArray();
    }

    [Fact] void should_not_write_initialization_for_legacy_observers() => _legacyDocument.Contains("initialization").ShouldBeFalse();
    [Fact] void should_leave_legacy_initialization_absent() => _legacy.Initialization.ShouldBeNull();
    [Fact] void should_keep_all_four_initialization_states() => _states.ShouldEqual(Enum.GetValues<ObserverInitializationState>());
    [Fact] void should_keep_every_refusal_reason() => _ordinary.Initialization.RefusalReasons.ShouldEqual("unfinished job", "unsettled handover");
    [Fact] void should_keep_initialization_on_the_joined_read() => _joined.Initialization.State.ShouldEqual(ObserverInitializationState.Initialized);
    [Fact] void should_keep_reasons_on_the_joined_read() => _joined.Initialization.RefusalReasons.ToArray().ShouldEqual(_ordinary.Initialization.RefusalReasons.ToArray());
    [Fact] void should_keep_the_new_running_state() => _ordinary.RunningState.ShouldEqual(ObserverRunningState.Initializing);
}
