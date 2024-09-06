// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Primitives;
using ProtoBuf;

namespace Cratis.Chronicle.Contracts.Observation.Reactors;

/// <summary>
/// Represents a message from the observer client.
/// </summary>
[ProtoContract]
public class ReactorMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReactorMessage"/> class.
    /// </summary>
    public ReactorMessage()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReactorMessage"/> class.
    /// </summary>
    /// <param name="content">The actual content.</param>
    public ReactorMessage(OneOf<RegisterReactor, ReactorResult> content)
    {
        Content = content;
    }

    /// <summary>
    /// Gets or sets the content of the message.
    /// </summary>
    [ProtoMember(1)]
    public OneOf<RegisterReactor, ReactorResult> Content { get; set; }
}
