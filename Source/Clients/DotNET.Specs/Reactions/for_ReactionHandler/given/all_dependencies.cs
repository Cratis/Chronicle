// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reactions.for_ReactionHandler.given;

public class all_dependencies : Specification
{
    protected ReactionId reaction_id;
    protected EventSequenceId event_sequence_id;
    protected Mock<IReactionInvoker> reaction_invoker;
    protected Mock<ICausationManager> causation_manager;

    void Establish()
    {
        reaction_id = Guid.NewGuid().ToString();
        event_sequence_id = Guid.NewGuid().ToString();
        reaction_invoker = new();
        causation_manager = new();
    }
}
