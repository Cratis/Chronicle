// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverHandler.given;

public class an_observer_handler : all_dependencies
{
    protected ObserverHandler handler;

    void Establish() => handler = new(
        observer_id,
        observer_name,
        event_sequence_id,
        observer_invoker.Object,
        causation_manager.Object);
}
