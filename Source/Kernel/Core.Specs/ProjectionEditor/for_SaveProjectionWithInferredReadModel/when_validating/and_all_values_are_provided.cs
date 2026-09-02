// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.ProjectionEditor.for_SaveProjectionWithInferredReadModel.when_validating;

public class and_all_values_are_provided : Specification
{
    readonly CommandScenario<SaveProjectionWithInferredReadModel> _scenario = ChronicleCommandScenario.For<SaveProjectionWithInferredReadModel>();
    CommandResult _result;

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        storage.HasEventStore(Arg.Any<EventStoreName>()).Returns(true);
        _scenario.Services.AddSingleton(storage);

        var readModels = Substitute.For<IReadModels>();
        readModels.GetDefinitions(Arg.Any<GetDefinitionsRequest>()).Returns(new GetDefinitionsResponse());
        _scenario.Services.AddSingleton(readModels);
    }

    async Task Because() => _result = await _scenario.Validate(
        new SaveProjectionWithInferredReadModel("some-event-store", "some-namespace", "some declaration", "Some Read Model"));

    [Fact] void should_be_valid() => _result.ShouldBeValid();
}
