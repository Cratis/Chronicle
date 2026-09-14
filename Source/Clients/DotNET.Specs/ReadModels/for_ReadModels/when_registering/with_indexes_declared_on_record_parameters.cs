// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_registering;

/// <summary>
/// The record shorthand — <c language="csharp">[Index]</c> on a positional parameter, which is the form the documentation
/// recommends and the one everybody writes.
/// </summary>
/// <remarks>
/// It collected nothing before, and did so silently: <see cref="IndexAttribute"/> is valid on a property and
/// on a parameter, C# binds it to the parameter on a positional record, and only properties were looked at.
/// The declaration compiled, registered, and produced no index — indistinguishable in the source from one
/// that works. Measured on a production store, every registered read model carried an empty index list.
/// </remarks>
public class with_indexes_declared_on_record_parameters : given.all_dependencies_with_configured_sink
{
    record MyReadModel(Guid Id, [property: Index] Guid ExplicitlyOnProperty, [Index] Guid OnParameter, Guid NotIndexed);

    Contracts.ReadModels.IReadModels _readModelsService;
    RegisterManyRequest _capturedRequest;
    IProjectionHandler _projectionHandler;

    void Establish()
    {
        _projectionHandler = Substitute.For<IProjectionHandler>();
        _projectionHandler.ReadModelType.Returns(typeof(MyReadModel));
        _projectionHandler.Id.Returns(new ProjectionId(Guid.NewGuid().ToString()));

        _projections.GetAllHandlers().Returns([_projectionHandler]);
        _reducers.GetAllHandlers().Returns([]);

        _readModelsService = Substitute.For<Contracts.ReadModels.IReadModels>();
        _services.ReadModels.Returns(_readModelsService);
        _readModelsService.When(_ => _.RegisterMany(Arg.Any<RegisterManyRequest>()))
            .Do(_ => _capturedRequest = _.Arg<RegisterManyRequest>());
    }

    async Task Because() => await _readModels.Register();

    IEnumerable<string> IndexedPaths => _capturedRequest.ReadModels[0].Indexes.Select(index => index.PropertyPath);

    [Fact] void should_index_the_property_targeted_declaration() => IndexedPaths.ShouldContain("ExplicitlyOnProperty");
    [Fact] void should_index_the_record_parameter_declaration() => IndexedPaths.ShouldContain("OnParameter");
    [Fact] void should_not_index_an_undeclared_property() => IndexedPaths.ShouldNotContain("NotIndexed");
    [Fact] void should_not_index_the_key() => IndexedPaths.ShouldNotContain("Id");
}
