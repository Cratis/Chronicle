// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Exception that is thrown when a specified CA certificate file does not exist.
/// </summary>
/// <param name="file">The file path.</param>
public class CertificateAuthorityDoesNotExist(string file) : Exception($"The specified CA certificate file '{ConnectionStringDiagnostics.Hide(file)}' does not exist.");
