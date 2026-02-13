// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Security;

/// <summary>
/// Represents the command for removing an application.
/// </summary>
[ProtoContract]
public class RemoveApplication
{
    /// <summary>
    /// The unique identifier for the client.
    /// </summary>
    [ProtoMember(1, IsRequired = true)]
    public Guid Id { get; set; } = Guid.Empty;
}
