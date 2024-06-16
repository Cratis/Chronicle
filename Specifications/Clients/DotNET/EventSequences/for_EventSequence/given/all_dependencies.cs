// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Contracts.EventSequences;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Identities;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class all_dependencies : Specification
{
    protected Mock<IEventTypes> event_types;
    protected Mock<IEventSerializer> event_serializer;
    protected Mock<ICausationManager> causation_manager;
    protected Mock<IIdentityProvider> identity_provider;
    protected Mock<IChronicleConnection> connection;
    protected Mock<IEventSequences> event_sequences;
    protected Mock<IServices> services;


    void Establish()
    {
        event_types = new();
        event_serializer = new();
        causation_manager = new();
        identity_provider = new();
        connection = new();
        event_sequences = new();
        services = new();
        connection.SetupGet(_ => _.Services).Returns(services.Object);
        services.SetupGet(_ => _.EventSequences).Returns(event_sequences.Object);
    }
}
