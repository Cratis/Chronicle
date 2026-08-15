// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Server.Authentication;

/// <summary>
/// Represents how many stored Data Protection keys depend on one certificate.
/// </summary>
/// <param name="KeyId">The thumbprint of the certificate the keys are encrypted to.</param>
/// <param name="Role">The position that certificate holds in the ring, or <see cref="EncryptionCertificateRole.Retired"/> when it holds none.</param>
/// <param name="KeyCount">The number of stored keys encrypted to it.</param>
public record DataProtectionKeyDependency(
    string KeyId,
    EncryptionCertificateRole Role,
    int KeyCount);
