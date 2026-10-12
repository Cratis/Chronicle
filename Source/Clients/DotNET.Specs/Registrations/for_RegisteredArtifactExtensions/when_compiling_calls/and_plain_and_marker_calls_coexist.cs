// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Registrations.for_RegisteredArtifactExtensions.when_compiling_calls;

public class and_plain_and_marker_calls_coexist : Specification
{
    IReactors _reactors;
    IReducers _reducers;

    void Establish()
    {
        _reactors = Substitute.For<IReactors>();
        _reducers = Substitute.For<IReducers>();
    }

    async Task Because()
    {
        await _reactors.Register<PlainReactor>();
        _reactors.GetHandlerFor<PlainReactor>();
        await _reactors.GetStateFor<PlainReactor>();
        await _reactors.Replay<PlainReactor>();
        await _reducers.Register<PlainReducer>();
        _reducers.GetHandlerFor<PlainReducer>();
        await _reducers.GetFailedPartitionsFor<PlainReducer>();
        await _reducers.GetStateFor<PlainReducer>();
        await _reducers.Replay<PlainReducer>();
        await _reactors.Register<MarkerReactor>();
        await _reactors.Register<MarkerReactorStruct>();
        await _reducers.Register<MarkerReducer, Model>();
        await _reducers.Register<MarkerReducerStruct, Model>();
    }

    [Fact] void should_bind_plain_reactor_registration_to_the_type_core() => _reactors.Received(1).Register(typeof(PlainReactor));
    [Fact] void should_bind_plain_reactor_handler_lookup_to_the_type_core() => _reactors.Received(1).GetHandlerFor(typeof(PlainReactor));
    [Fact] void should_bind_plain_reactor_state_to_the_type_core() => _reactors.Received(1).GetStateFor(typeof(PlainReactor));
    [Fact] void should_bind_plain_reactor_replay_to_the_type_core() => _reactors.Received(1).Replay(typeof(PlainReactor));
    [Fact] void should_bind_plain_reducer_registration_to_the_type_core() => _reducers.Received(1).Register(typeof(PlainReducer));
    [Fact] void should_bind_plain_reducer_handler_lookup_to_the_type_core() => _reducers.Received(1).GetHandlerFor(typeof(PlainReducer));
    [Fact] void should_bind_plain_reducer_failed_partitions_to_the_type_core() => _reducers.Received(1).GetFailedPartitionsFor(typeof(PlainReducer));
    [Fact] void should_bind_plain_reducer_state_to_the_type_core() => _reducers.Received(1).GetStateFor(typeof(PlainReducer));
    [Fact] void should_bind_plain_reducer_replay_to_the_type_core() => _reducers.Received(1).Replay(typeof(PlainReducer));
    [Fact] void should_preserve_the_marker_reactor_instance_member() => _reactors.Received(1).Register<MarkerReactor>();
    [Fact] void should_preserve_the_struct_reactor_instance_member() => _reactors.Received(1).Register<MarkerReactorStruct>();
    [Fact] void should_preserve_the_marker_reducer_instance_member() => _reducers.Received(1).Register<MarkerReducer, Model>();
    [Fact] void should_preserve_the_struct_reducer_instance_member() => _reducers.Received(1).Register<MarkerReducerStruct, Model>();

    public class PlainReactor;

    public class PlainReducer;

    public class MarkerReactor : IReactor;

    public struct MarkerReactorStruct : IReactor;

    public class MarkerReducer : IReducerFor<Model>;

    public struct MarkerReducerStruct : IReducerFor<Model>;

    public record Model;
}
