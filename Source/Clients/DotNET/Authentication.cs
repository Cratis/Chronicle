// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle;

/// <summary>
/// Represents the authentication configuration.
/// </summary>
public class Authentication
{
    /// <summary>
    /// Gets or sets the authentication authority URL. If not configured, uses the internal OAuth authority.
    /// </summary>
    public string? Authority { get; set; }
}
