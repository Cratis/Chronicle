// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Contracts.Primitives;
using ProtoBuf;

namespace Aksio.Cratis.Kernel.Contracts.Observation;

/// <summary>
/// Represents a message from the observer client.
/// </summary>
[ProtoContract]
public class ObserverClientMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ObserverClientMessage"/> class.
    /// </summary>
    public ObserverClientMessage()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ObserverClientMessage"/> class.
    /// </summary>
    /// <param name="content">The actual content.</param>
    public ObserverClientMessage(OneOf<RegisterObserver, ObservationResult> content)
    {
        Content = content;
    }

    /// <summary>
    /// Gets or sets the content of the message.
    /// </summary>
    [ProtoMember(1)]
    public OneOf<RegisterObserver, ObservationResult> Content { get; set; }
}
