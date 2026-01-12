// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.ReadModels;

/// <summary>
/// Represents the response for getting read model occurrences.
/// </summary>
[ProtoContract]
public class GetOccurrencesResponse
{
    /// <summary>
    /// Gets or sets collection of <see cref="ReadModelOccurrence"/>.
    /// </summary>
    [ProtoMember(1, IsRequired = true)]
    public IEnumerable<ReadModelOccurrence> Occurrences { get; set; } = [];
}
