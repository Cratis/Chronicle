// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_registering_event_targets.given;

public abstract class an_event_target<TTarget> : for_ReadModels.given.all_dependencies
{
    protected RegisterManyRequest _request;
    protected Exception _exception;

    protected virtual bool UseReducer => false;

    void Establish()
    {
        _eventTypes.HasFor(Arg.Any<EventTypeId>()).Returns(true);
        _schemaGenerator.Generate(Arg.Any<Type>()).Returns(new JsonSchema());
        _reducers.GetAllHandlers().Returns([]);
        _projections.GetAllHandlers().Returns([]);

        if (UseReducer)
        {
            var reducer = Substitute.For<IReducerHandler>();
            reducer.ReadModelType.Returns(typeof(TTarget));
            reducer.Id.Returns(new ReducerId("a-reducer"));
            _reducers.GetAllHandlers().Returns([reducer]);
        }
        else
        {
            var projection = Substitute.For<IProjectionHandler>();
            projection.ReadModelType.Returns(typeof(TTarget));
            projection.Id.Returns(new ProjectionId("a-projection"));
            _projections.GetAllHandlers().Returns([projection]);
        }

        var service = Substitute.For<Contracts.ReadModels.IReadModels>();
        _services.ReadModels.Returns(service);
        service.When(_ => _.RegisterMany(Arg.Any<RegisterManyRequest>())).Do(_ => _request = _.Arg<RegisterManyRequest>());
    }

    protected async Task Exercise() => _exception = await Catch.Exception(() => _readModels.Register());
}
