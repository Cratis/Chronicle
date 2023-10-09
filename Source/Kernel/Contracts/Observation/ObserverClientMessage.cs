// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OneOf;
using ProtoBuf;

namespace Aksio.Cratis.Kernel.Contracts.Observation;

/// <summary>
/// Represents a message from the observer client.
/// </summary>
[ProtoContract]
public class ObserverClientMessage
{
    /// <summary>
    /// Gets or sets the register type of message.
    /// </summary>
    [ProtoMember(1, IsRequired = false)]
    public RegisterObserver Register { get; set; }

    /// <summary>
    /// Gets or sets the observation result type of message.
    /// </summary>
    [ProtoMember(2, IsRequired = false)]
    public ObservationResult ObservationResult { get; set; }

    /// <summary>
    /// Gets or sets the actual message as a <see cref="OneOf"/>.
    /// </summary>
    [ProtoIgnore]
    public OneOf<RegisterObserver, ObservationResult> Result
    {
        get
        {
            return Register ?? (OneOf<RegisterObserver, ObservationResult>)ObservationResult;
        }
        set
        {
            if (value.IsT0)
            {
                Register = value.AsT0;
                ObservationResult = null!;
            }
            else
            {
                Register = null!;
                ObservationResult = value.AsT1;
            }
        }
    }
}
