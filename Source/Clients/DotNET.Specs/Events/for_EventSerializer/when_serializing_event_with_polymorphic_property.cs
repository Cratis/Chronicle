// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Serialization;

namespace Cratis.Chronicle.Events.for_EventSerializer;

public class when_serializing_event_with_polymorphic_property : Specification
{
    public interface IShape;

    [DerivedType("circle")]
    public record Circle(int Radius) : IShape;

    public record EventWithShape(IShape Shape);

    EventSerializer _serializer;
    JsonObject _result;

    void Establish()
    {
        var clientArtifacts = Substitute.For<IClientArtifactsProvider>();
        clientArtifacts.AdditionalEventInformationProviders.Returns([]);
        var activator = Substitute.For<IClientArtifactsActivator>();
        var eventTypes = Substitute.For<IEventTypes>();

        // Pass options WITHOUT the derived-type converter to prove the serializer adds it when missing.
        _serializer = new EventSerializer(clientArtifacts, activator, eventTypes, new JsonSerializerOptions());
    }

    async Task Because() => _result = await _serializer.Serialize(new EventWithShape(new Circle(5)));

    [Fact] void should_write_the_derived_type_discriminator() => _result.ToJsonString().Contains("_derivedTypeId").ShouldBeTrue();
    [Fact] void should_identify_the_concrete_derived_type() => _result.ToJsonString().Contains("circle").ShouldBeTrue();
    [Fact] void should_write_the_concrete_subtype_property() => _result.ToJsonString().Contains("\"radius\":5").ShouldBeTrue();
}
