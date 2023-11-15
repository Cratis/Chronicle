// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Auditing;
using Aksio.Cratis.EventSequences;

namespace Aksio.Cratis.Observation.for_ObserverHandler.given;

public class all_dependencies : Specification
{
    protected ObserverId observer_id;
    protected ObserverName observer_name;
    protected EventSequenceId event_sequence_id;
    protected Mock<IObserverInvoker> observer_invoker;
    protected Mock<ICausationManager> causation_manager;
    protected Mock<IEventSerializer> event_serializer;

    void Establish()
    {
        observer_id = Guid.NewGuid();
        observer_name = "My Observer";
        event_sequence_id = Guid.NewGuid();
        observer_invoker = new();
        causation_manager = new();
        event_serializer = new();
    }
}
