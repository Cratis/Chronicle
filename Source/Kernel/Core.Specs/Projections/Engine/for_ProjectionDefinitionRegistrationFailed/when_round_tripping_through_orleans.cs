// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Setup.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionDefinitionRegistrationFailed;

public class when_round_tripping_through_orleans : Specification
{
    Engine.ProjectionDefinitionRegistrationFailed _original;
    Engine.ProjectionDefinitionRegistrationFailed _result;

    void Establish() => _original = new Engine.ProjectionDefinitionRegistrationFailed(
        "the-projection",
        new InvalidOperationException("the root cause"));

    void Because()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new JsonSerializerOptions());
        services.AddConceptSerializer();
        var serializer = services.BuildServiceProvider().GetRequiredService<Serializer>();
        _result = serializer.Deserialize<Engine.ProjectionDefinitionRegistrationFailed>(serializer.SerializeToArray(_original));
    }

    [Fact] void should_keep_the_attributed_identifier() => _result.Identifier.ShouldEqual(_original.Identifier);
    [Fact] void should_keep_the_root_cause() => _result.GetBaseException().Message.ShouldEqual("the root cause");
}
