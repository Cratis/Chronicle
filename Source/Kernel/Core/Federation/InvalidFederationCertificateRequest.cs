// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// The exception that is thrown when a certificate request cannot be honored.
/// </summary>
/// <param name="reason">Why the request is invalid. Never contains a secret.</param>
public class InvalidFederationCertificateRequest(string reason) : Exception($"The federation certificate request is invalid: {reason}");
