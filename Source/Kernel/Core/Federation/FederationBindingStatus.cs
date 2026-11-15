// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Consumer-side operational state of a binding.
/// </summary>
/// <param name="State">One of <c language="text">Unknown</c>, <c language="text">Connecting</c>, <c language="text">Streaming</c>, <c language="text">Backoff</c>, <c language="text">Denied</c>, <c language="text">Disabled</c>.</param>
/// <param name="LastSourceSequenceNumber">The last source sequence number appended to the inbox.</param>
/// <param name="RemoteTailSequenceNumber">The remote outbox tail last observed.</param>
/// <param name="LastDelivery">When events were last appended.</param>
/// <param name="LastError">The last error, never containing a secret.</param>
public record FederationBindingStatus(string State, ulong LastSourceSequenceNumber, ulong RemoteTailSequenceNumber, DateTimeOffset? LastDelivery, string? LastError)
{
    /// <summary>
    /// Gets the status of a binding nothing has been recorded for.
    /// </summary>
    public static FederationBindingStatus Unknown { get; } = new("Unknown", 0, 0, null, null);

    /// <summary>
    /// Gets the lag, the number of outbox sequence numbers not yet consumed.
    /// </summary>
    public ulong Lag => RemoteTailSequenceNumber > LastSourceSequenceNumber ? RemoteTailSequenceNumber - LastSourceSequenceNumber : 0;
}
