// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.ReadModels;

/// <summary>
/// Represents one property of an ordered read-model index.
/// </summary>
[ProtoContract]
public class IndexProperty
{
    /// <summary>
    /// Gets or sets the indexed property path.
    /// </summary>
    [ProtoMember(1)]
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the property is indexed in descending order.
    /// </summary>
    [ProtoMember(2)]
    public bool Descending { get; set; }
}
