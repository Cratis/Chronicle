// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using Cratis.Serialization;

namespace Cratis.Chronicle.Events.Constraints.for_ClosesStreamConstraintsProvider.given;

public class a_closing_provider : Specification
{
    protected IClientArtifactsProvider _artifacts;
    protected IEventTypes _eventTypes;
    protected ClosesStreamConstraintsProvider _provider;

    void Establish()
    {
        _artifacts = Substitute.For<IClientArtifactsProvider>();
        _eventTypes = Substitute.For<IEventTypes>();
        _provider = new(_artifacts, _eventTypes, new DefaultNamingPolicy());
    }

    protected void Register<T>()
    {
        var eventType = new EventType(typeof(T).Name, EventTypeGeneration.First);
        _eventTypes.GetEventTypeFor(typeof(T)).Returns(eventType);
        _eventTypes.GetSchemaFor(eventType.Id).Returns(JsonSchema.FromType<T>());
    }
}
