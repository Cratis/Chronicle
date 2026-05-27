// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.EventSequences;

/// <summary>
/// Represents the response from completing a stream.
/// </summary>
[ProtoContract]
public class CompleteStreamResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether the stream was completed successfully by this call.
    /// </summary>
    [ProtoMember(1, IsRequired = true)]
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets the tail sequence number captured at the time of completion.
    /// </summary>
    /// <remarks>
    /// Only meaningful when <see cref="IsSuccess"/> is <c>true</c>. When completion failed because the stream had
    /// already been completed, the previously captured tail sequence number is not returned through this response.
    /// </remarks>
    [ProtoMember(2)]
    public ulong SequenceNumber { get; set; }

    /// <summary>
    /// Gets or sets the error encountered when <see cref="IsSuccess"/> is <c>false</c>.
    /// </summary>
    [ProtoMember(3)]
    public CompleteStreamError? Error { get; set; }
}
