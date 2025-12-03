// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation.Reducers;

/// <summary>
/// Represents a message from the observer client.
/// </summary>
[ProtoContract]
public class ReducerMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReducerMessage"/> class.
    /// </summary>
    public ReducerMessage()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReducerMessage"/> class.
    /// </summary>
    /// <param name="content">The actual content.</param>
    public ReducerMessage(OneOf<RegisterReducer, ReducerResult> content)
    {
        Content = content;
    }

    /// <summary>
    /// Gets or sets the content of the message.
    /// </summary>
    [ProtoMember(1)]
    public OneOf<RegisterReducer, ReducerResult> Content { get; set; }
}
