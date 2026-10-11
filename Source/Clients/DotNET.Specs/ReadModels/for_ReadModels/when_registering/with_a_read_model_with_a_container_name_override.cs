// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_registering;

public class with_a_read_model_with_a_container_name_override : given.all_dependencies_with_configured_sink
{
    record Overridden(string Name);

    record Defaulted(string Name);

    RegisterManyRequest _capturedRequest;

    void Establish()
    {
        var handlers = new[] { Handler(typeof(Overridden), "overridden"), Handler(typeof(Defaulted), "defaulted") };
        _projections.GetAllHandlers().Returns(handlers);
        ((IKnowReadModelContainerNames)_projections).GetContainerNameOverride(typeof(Overridden)).Returns("custom-container");
        ((IKnowReadModelContainerNames)_projections).GetContainerNameOverride(typeof(Defaulted)).Returns((string)null);
        _reducers.GetAllHandlers().Returns([]);

        var readModelsService = Substitute.For<Contracts.ReadModels.IReadModels>();
        _services.ReadModels.Returns(readModelsService);
        readModelsService.When(_ => _.RegisterMany(Arg.Any<RegisterManyRequest>()))
            .Do(_ => _capturedRequest = _.Arg<RegisterManyRequest>());
    }

    async Task Because() => await _readModels.Register();

    [Fact] void should_use_the_override_as_container_name() => Definition(typeof(Overridden)).ContainerName.ShouldEqual("custom-container");
    [Fact] void should_keep_the_full_name_as_identifier() => Definition(typeof(Overridden)).Type.Identifier.ShouldEqual(typeof(Overridden).FullName);
    [Fact] void should_use_the_naming_policy_when_there_is_no_override() => Definition(typeof(Defaulted)).ContainerName.ShouldEqual(_namingPolicy.GetReadModelName(typeof(Defaulted)));
    [Fact] void should_keep_the_full_name_as_identifier_when_there_is_no_override() => Definition(typeof(Defaulted)).Type.Identifier.ShouldEqual(typeof(Defaulted).FullName);

    static IProjectionHandler Handler(Type readModelType, string id)
    {
        var handler = Substitute.For<IProjectionHandler>();
        handler.ReadModelType.Returns(readModelType);
        handler.Id.Returns(new ProjectionId(id));
        return handler;
    }

    ReadModelDefinition Definition(Type readModelType) => _capturedRequest.ReadModels.Single(_ => _.Type.Identifier == readModelType.FullName);
}
