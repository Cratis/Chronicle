// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Projections.for_Projections.replay_tests;

public class when_replaying_by_projection_type : given.all_dependencies
{
    readonly ProjectionId _projectionId = "93c0c8ed-f2cd-49a2-b5b9-f2f4e1b7b5d6";
    IProjectionHandler _handler;

    void Establish()
    {
        _handler = Substitute.For<IProjectionHandler>();
        _handler.Id.Returns(_projectionId);

        _handlersByType[typeof(MyProjection)] = _handler;

        _observers.Replay(Arg.Any<Replay>()).Returns(Task.CompletedTask);
    }

    async Task Because() => await _projections.Replay<MyProjection>();

    [Fact]
    void should_call_replay_with_correct_parameters() =>
        _observers
            .Received(1)
            .Replay(Arg.Is<Replay>(r =>
                r.EventStore == _eventStore.Name.Value &&
                r.Namespace == _eventStore.Namespace.Value &&
                r.ObserverId == _projectionId.Value &&
                r.EventSequenceId == string.Empty));

    class MyModel
    {
        public string Id { get; set; } = string.Empty;
    }

    class MyProjection : IProjectionFor<MyModel>
    {
        public void Define(IProjectionBuilderFor<MyModel> builder)
        {
        }
    }
}
