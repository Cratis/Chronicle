// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Auditing;
using Aksio.Cratis.Connections;
using Aksio.Cratis.Identities;

namespace Aksio.Cratis.EventSequences.for_EventSequence.given;

public class all_dependencies : Specification
{
    protected Mock<IEventTypes> event_types;
    protected Mock<IEventSerializer> event_serializer;
    protected Mock<IConnection> connection;
    protected Mock<IObserversRegistrar> observers_registrar;
    protected Mock<ICausationManager> causation_manager;
    protected Mock<IIdentityProvider> identity_provider;
    protected Mock<IExecutionContextManager> execution_context_manager;

    protected ExecutionContext execution_context;

    void Establish()
    {
        event_types = new();
        event_serializer = new();
        connection = new();
        observers_registrar = new();
        causation_manager = new();
        identity_provider = new();
        execution_context_manager = new();

        execution_context = new ExecutionContext(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid().ToString());

        execution_context_manager
            .SetupGet(_ => _.Current)
            .Returns(execution_context);
    }
}
