// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// The body of a renewal request sent over mutual TLS.
/// </summary>
/// <param name="Csr">The PKCS#10 certificate request for the new key, DER encoded and base64.</param>
public record FederationRenewRequest(string Csr);
