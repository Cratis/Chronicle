// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Events;

/// <summary>
/// A structured event tag with an exact value.
/// </summary>
[ProtoContract]
public class NamedTag
{
    /// <summary>
    /// Gets or sets the tag name.
    /// </summary>
    [ProtoMember(1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the exact tag value.
    /// </summary>
    [ProtoMember(2)]
    public string Value { get; set; } = string.Empty;
}
