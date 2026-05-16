// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents the encryption configuration within compliance settings.
/// </summary>
public class Encryption
{
    /// <summary>
    /// Gets the optional storage configuration for encryption keys.
    /// When not configured, the general <see cref="Storage"/> is used as the default.
    /// </summary>
    public Storage? Storage { get; init; }
}
