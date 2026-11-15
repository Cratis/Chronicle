// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Compliance;

/// <summary>
/// Coverage of an encryption key erasure. An empty list means all eligible event stores were covered.
/// </summary>
[ProtoContract]
public class DeleteEncryptionKeyResponse
{
    /// <summary>
    /// Gets or sets the event stores not erased because authorization was denied or unavailable.
    /// </summary>
    [ProtoMember(1)]
    public IList<string> NotCoveredEventStores { get; set; } = [];
}
