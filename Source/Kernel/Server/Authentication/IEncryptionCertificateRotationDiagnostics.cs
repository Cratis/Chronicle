// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.Authentication;

/// <summary>
/// Defines the diagnostic that tells an operator where a certificate rotation stands.
/// </summary>
public interface IEncryptionCertificateRotationDiagnostics
{
    /// <summary>
    /// Reports the certificate ring and what the stored Data Protection keys still depend on.
    /// </summary>
    /// <returns>The current <see cref="EncryptionCertificateRotationReport"/>.</returns>
    EncryptionCertificateRotationReport GetReport();
}
