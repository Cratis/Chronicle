// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_removing_child_collection;

public class ConversationProjection : IProjectionFor<Conversation>
{
    public static bool WithReactions { get; set; }

    public void Define(IProjectionBuilderFor<Conversation> builder)
    {
        builder
            .From<ConversationStarted>(b => b.Set(m => m.Name).To(e => e.Name))
            .Children(m => m.Comments, c => c
                .IdentifiedBy(m => m.CommentId)
                .From<CommentAdded>(b => b.UsingKey(e => e.CommentId)));

        if (WithReactions)
        {
            builder.Children(m => m.Reactions, c => c
                .IdentifiedBy(m => m.ReactionId)
                .From<ReactionGiven>(b => b.UsingKey(e => e.ReactionId)));
        }
    }
}
