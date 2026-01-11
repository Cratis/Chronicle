// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Security;

/// <summary>
/// Represents the response containing applications.
/// </summary>
[ProtoContract]
public class ApplicationsResponse
{
    /// <summary>
    /// Gets or sets the collection of applications.
    /// </summary>
    [ProtoMember(1)]
    public IList<Application> Applications { get; set; } = [];
}
