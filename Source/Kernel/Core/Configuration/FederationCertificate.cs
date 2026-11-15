// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents a named PKCS#12 certificate used as a federation client credential.
/// </summary>
public class FederationCertificate
{
    /// <summary>
    /// Gets or inits the path to the PKCS#12 file.
    /// </summary>
    public string? Path { get; init; }

    /// <summary>
    /// Gets or inits the path to a file holding the password. The password itself is never configured inline.
    /// </summary>
    public string? PasswordPath { get; init; }
}
