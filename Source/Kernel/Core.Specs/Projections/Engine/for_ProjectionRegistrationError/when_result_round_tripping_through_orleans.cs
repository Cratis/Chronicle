// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Setup.Serialization;
using Cratis.Monads;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionRegistrationError;

public class when_result_round_tripping_through_orleans : Specification
{
    Result<ProjectionRegistrationError> _original;
    ProjectionRegistrationError _error;

    void Establish()
    {
        _original = Result.Failed(new ProjectionRegistrationError(new Dictionary<ProjectionId, Exception>
        {
            [(ProjectionId)"first-projection"] = new InvalidOperationException("first root cause"),
            [(ProjectionId)"second-projection"] = new InvalidOperationException("second root cause")
        }));
    }

    void Because()
    {
        var services = new ServiceCollection();
        var options = new JsonSerializerOptions();
        services.AddSingleton(options);
        services.AddConceptSerializer();
        services.AddSingleton(Substitute.For<Cratis.Chronicle.Json.IExpandoObjectConverter>());
        services.AddSingleton(Substitute.For<Storage.IStorage>());
        services.AddCustomSerializers();
        services.AddSerializer(builder => builder.AddJsonSerializer(type => type.Namespace == "OneOf.Types", options));
        var serializer = services.BuildServiceProvider().GetRequiredService<Serializer>();
        var result = serializer.Deserialize<Result<ProjectionRegistrationError>>(serializer.SerializeToArray(_original));
        result.TryGetError(out _error);
    }

    [Fact] void should_keep_every_failed_identifier() => _error.Failures.Keys.ShouldContainOnly((ProjectionId)"first-projection", (ProjectionId)"second-projection");
    [Fact] void should_keep_the_first_root_cause() => _error.Failures[(ProjectionId)"first-projection"].GetBaseException().Message.ShouldEqual("first root cause");
    [Fact] void should_keep_the_second_root_cause() => _error.Failures[(ProjectionId)"second-projection"].GetBaseException().Message.ShouldEqual("second root cause");
}
