// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grains.Security;

/// <summary>
/// Exception that gets thrown when an encryption certificate is required but not configured.
/// </summary>
public class EncryptionCertificateNotConfigured()
    : Exception(
        "An encryption certificate is required for encrypting sensitive data. " +
        "Configure 'EncryptionCertificate:CertificatePath' and 'EncryptionCertificate:CertificatePassword' " +
        "in your configuration.");
