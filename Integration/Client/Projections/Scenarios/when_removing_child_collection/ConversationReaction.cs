// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_removing_child_collection;

public class ConversationReaction
{
    public Guid ReactionId { get; set; }
    public string Emoji { get; set; } = string.Empty;
}
