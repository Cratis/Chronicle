// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security;

/// <summary>
/// Exception that gets thrown when a previous encryption certificate is listed without a certificate path.
/// </summary>
public class EncryptionCertificateWithoutPath()
    : Exception(
        "An entry under 'EncryptionCertificate:Previous' has no 'CertificatePath'. Every previous certificate " +
        "must name the file it is loaded from; an entry that names nothing contributes nothing to the ring.");
