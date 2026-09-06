// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Setup.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;

namespace Cratis.Chronicle.Projections.for_SomeProjectionDefinitionsFailedToRegister;

public class when_round_tripping_through_orleans : Specification
{
    SomeProjectionDefinitionsFailedToRegister _original;
    SomeProjectionDefinitionsFailedToRegister _result;

    void Establish()
    {
        _original = new(
            (EventStoreName)"event-store",
            new Dictionary<ProjectionId, Exception>
            {
                [(ProjectionId)"first-projection"] = new ProjectionDefinitionRegistrationFailed(
                    "first-projection",
                    new InvalidOperationException("first root cause")),
                [(ProjectionId)"second-projection"] = new ProjectionDefinitionRegistrationFailed(
                    "second-projection",
                    new InvalidOperationException("second root cause"))
            });
    }

    void Because()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new JsonSerializerOptions());
        services.AddConceptSerializer();
        var serializer = services.BuildServiceProvider().GetRequiredService<Serializer>();
        _result = serializer.Deserialize<SomeProjectionDefinitionsFailedToRegister>(serializer.SerializeToArray(_original));
    }

    [Fact] void should_keep_every_failed_identifier() => _result.Failures.Keys.ShouldContainOnly((ProjectionId)"first-projection", (ProjectionId)"second-projection");
    [Fact] void should_keep_the_first_root_cause() => _result.Failures[(ProjectionId)"first-projection"].GetBaseException().Message.ShouldEqual("first root cause");
    [Fact] void should_keep_the_second_root_cause() => _result.Failures[(ProjectionId)"second-projection"].GetBaseException().Message.ShouldEqual("second root cause");
}
