// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Primitives;
using ProtoBuf;

namespace Cratis.Chronicle.Contracts.Observation.Reactions;

/// <summary>
/// Represents a message from the observer client.
/// </summary>
[ProtoContract]
public class ReactionMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReactionMessage"/> class.
    /// </summary>
    public ReactionMessage()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReactionMessage"/> class.
    /// </summary>
    /// <param name="content">The actual content.</param>
    public ReactionMessage(OneOf<RegisterReaction, ReactionResult> content)
    {
        Content = content;
    }

    /// <summary>
    /// Gets or sets the content of the message.
    /// </summary>
    [ProtoMember(1)]
    public OneOf<RegisterReaction, ReactionResult> Content { get; set; }
}
