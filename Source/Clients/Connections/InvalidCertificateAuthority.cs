// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Exception that is thrown when a specified CA certificate file cannot be read or contains no certificates.
/// </summary>
/// <param name="file">The file path.</param>
public class InvalidCertificateAuthority(string file) : Exception($"The specified CA certificate file '{ConnectionStringDiagnostics.Hide(file)}' could not be read or contains no certificates.");
