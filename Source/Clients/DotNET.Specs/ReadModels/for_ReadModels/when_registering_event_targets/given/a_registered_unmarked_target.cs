// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_registering_event_targets.given;

public class a_registered_unmarked_target<TTarget> : for_ReadModels.given.all_dependencies
{
    protected Exception _bulkError;
    protected Exception _genericError;
    protected readonly List<ReadModelDefinition> _definitions = [];

    void Establish()
    {
        _eventTypes.HasFor(typeof(TTarget)).Returns(true);
        _eventTypes.HasFor(new EventTypeId("registered-target")).Returns(true);
        _eventTypes.GetEventTypeFor(typeof(TTarget)).Returns(new EventType("registered-target", 4));
        _schemaGenerator.Generate(Arg.Any<Type>()).Returns(new JsonSchema());
        _schemaGenerator.GenerateLegacyEventType(Arg.Any<Type>()).Returns(new JsonSchema());
        var handler = Substitute.For<IProjectionHandler>();
        handler.ReadModelType.Returns(typeof(TTarget));
        handler.Id.Returns(new ProjectionId("target-projection"));
        _projections.GetAllHandlers().Returns([handler]);
        _reducers.GetAllHandlers().Returns([]);
        var service = Substitute.For<Contracts.ReadModels.IReadModels>();
        _services.ReadModels.Returns(service);
        service.When(instance => instance.RegisterMany(Arg.Any<RegisterManyRequest>()))
            .Do(call => _definitions.AddRange(call.Arg<RegisterManyRequest>().ReadModels));
    }

    protected async Task RegisterBoth()
    {
        _bulkError = await Catch.Exception(_readModels.Register);
        _genericError = await Catch.Exception(_readModels.Register<TTarget>);
    }
}
