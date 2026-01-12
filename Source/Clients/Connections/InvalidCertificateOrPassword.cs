// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Exception that is thrown when a specified TLS certificate file is invalid or the password is incorrect.
/// </summary>
/// <param name="file">The file path.</param>
public class InvalidCertificateOrPassword(string file) : Exception($"The specified TLS certificate file '{file}' is invalid or the password is incorrect.");
