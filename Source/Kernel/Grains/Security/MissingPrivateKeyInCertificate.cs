// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grains.Security;

/// <summary>
/// Exception that gets thrown when a certificate does not contain an RSA private key.
/// </summary>
public class MissingPrivateKeyInCertificate() : Exception("Certificate does not contain an RSA private key.");
