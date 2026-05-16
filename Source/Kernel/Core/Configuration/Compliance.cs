// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents the compliance configuration.
/// </summary>
public class Compliance
{
    /// <summary>
    /// Gets the encryption configuration for compliance data, such as encryption keys.
    /// </summary>
    public Encryption Encryption { get; init; } = new Encryption();
}
