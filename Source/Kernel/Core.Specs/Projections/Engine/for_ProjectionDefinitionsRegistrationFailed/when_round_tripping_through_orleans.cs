// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Setup.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionDefinitionsRegistrationFailed;

public class when_round_tripping_through_orleans : Specification
{
    Engine.ProjectionDefinitionsRegistrationFailed _original;
    Engine.ProjectionDefinitionsRegistrationFailed _result;

    void Establish()
    {
        var firstFailure = new Engine.ProjectionDefinitionRegistrationFailed(
            "first-projection",
            new InvalidOperationException("first root cause"));
        var secondFailure = new Engine.ProjectionDefinitionRegistrationFailed(
            "second-projection",
            new InvalidOperationException("second root cause"));
        _original = new(new Dictionary<ProjectionId, Engine.ProjectionDefinitionRegistrationFailed>
        {
            [firstFailure.Identifier] = firstFailure,
            [secondFailure.Identifier] = secondFailure
        });
    }

    void Because()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new JsonSerializerOptions());
        services.AddConceptSerializer();
        var serializer = services.BuildServiceProvider().GetRequiredService<Serializer>();
        _result = serializer.Deserialize<Engine.ProjectionDefinitionsRegistrationFailed>(serializer.SerializeToArray(_original));
    }

    [Fact] void should_keep_every_failed_identifier() => _result.Failures.Keys.ShouldContainOnly((ProjectionId)"first-projection", (ProjectionId)"second-projection");
    [Fact] void should_keep_the_first_root_cause() => _result.Failures[(ProjectionId)"first-projection"].GetBaseException().Message.ShouldEqual("first root cause");
    [Fact] void should_keep_the_second_root_cause() => _result.Failures[(ProjectionId)"second-projection"].GetBaseException().Message.ShouldEqual("second root cause");
}
