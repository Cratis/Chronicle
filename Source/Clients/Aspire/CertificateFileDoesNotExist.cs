// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Aspire;

/// <summary>
/// The exception that is thrown when a certificate file configured for the Chronicle resource does not exist on the host.
/// </summary>
/// <param name="certificatePath">The certificate path as configured.</param>
/// <param name="resolvedPath">The absolute path the configured path resolved to.</param>
public class CertificateFileDoesNotExist(string certificatePath, string resolvedPath)
    : Exception($"The certificate file '{certificatePath}' does not exist. It resolved to '{resolvedPath}' - a relative path resolves against the AppHost directory. Docker would create a directory at that path and the Chronicle container would then report that no certificate is configured.");
