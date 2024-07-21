// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Aggregates.for_AggregateRoot.given;

public class all_dependencies : Specification
{
    protected Mock<IAggregateRootEventHandlers> event_handlers;
    protected Mock<IEventSequence> event_sequence;
    protected Mock<ICausationManager> causation_manager;

    void Establish()
    {
        event_handlers = new();
        event_sequence = new();
        causation_manager = new();
    }
}
