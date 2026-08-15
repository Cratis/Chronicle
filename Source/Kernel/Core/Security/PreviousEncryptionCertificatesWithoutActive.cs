// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security;

/// <summary>
/// Exception that gets thrown when previous encryption certificates are configured without an active one.
/// </summary>
public class PreviousEncryptionCertificatesWithoutActive()
    : Exception(
        "'EncryptionCertificate:Previous' is configured, but 'EncryptionCertificate:CertificatePath' is not. " +
        "Previous certificates only decrypt; without an active certificate there is nothing to protect new " +
        "values with, and the ring cannot be used.");
