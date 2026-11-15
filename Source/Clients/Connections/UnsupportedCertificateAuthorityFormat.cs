// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Exception that is thrown when a specified CA certificate file contains an OpenSSL TRUSTED CERTIFICATE block, which is not supported.
/// </summary>
/// <param name="file">The file path.</param>
public class UnsupportedCertificateAuthorityFormat(string file) : Exception($"The specified CA certificate file '{ConnectionStringDiagnostics.Hide(file)}' contains a 'TRUSTED CERTIFICATE' block, which is not supported. Use 'CERTIFICATE' or 'PKCS7' blocks instead.");
