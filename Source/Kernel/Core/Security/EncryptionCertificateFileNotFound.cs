// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security;

/// <summary>
/// Exception that gets thrown when a certificate in the encryption-certificate ring is configured with a path that holds no file.
/// </summary>
/// <param name="certificatePath">The configured path.</param>
/// <param name="role">The position the certificate was configured to hold.</param>
public class EncryptionCertificateFileNotFound(string certificatePath, EncryptionCertificateRole role)
    : Exception(
        $"The {role.ToString().ToLowerInvariant()} encryption certificate is configured as '{certificatePath}', but there is no file there. " +
        "Chronicle refuses to start rather than run with a ring that is missing a certificate, which would make everything that " +
        "certificate protects unreadable without reporting it.");
