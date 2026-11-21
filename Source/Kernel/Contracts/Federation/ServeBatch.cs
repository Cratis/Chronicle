// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Federation;

/// <summary>
/// Represents the dialer's answer to a <see cref="ServePullRequest"/>.
/// </summary>
[ProtoContract]
public class ServeBatch
{
    /// <summary>
    /// Gets or sets the id of the request this answers.
    /// </summary>
    [ProtoMember(1)]
    public ulong RequestId { get; set; }

    /// <summary>
    /// Gets or sets the batch, with the same meaning as the answer to a pull.
    /// </summary>
    [ProtoMember(2)]
    public FederatedEventBatchResponse Batch { get; set; } = new();
}
