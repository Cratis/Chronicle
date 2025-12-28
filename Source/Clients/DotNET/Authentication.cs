// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle;

/// <summary>
/// Represents the authentication configuration.
/// </summary>
public class Authentication
{
    /// <summary>
    /// Gets or sets whether authentication is enabled.
    /// </summary>
    public string? Authority { get; set; }

    /// <summary>
    /// Gets or sets whether to use the internal OAuth authority.
    /// </summary>
    public bool UseInternalAuthority { get; set; } = true;
}
