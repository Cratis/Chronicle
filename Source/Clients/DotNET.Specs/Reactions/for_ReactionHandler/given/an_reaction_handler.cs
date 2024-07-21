// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactions.for_ReactionHandler.given;

public class an_reaction_handler : all_dependencies
{
    protected ReactionHandler handler;

    void Establish() => handler = new(
        reaction_id,
        event_sequence_id,
        reaction_invoker.Object,
        causation_manager.Object);
}
