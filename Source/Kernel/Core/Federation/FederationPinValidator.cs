// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Validates the certificate of a producer solely by its pinned public key. The chain, the issuer, the name and the
/// validity period are deliberately never consulted: trust is the pin and nothing else.
/// </summary>
public static class FederationPinValidator
{
    /// <summary>
    /// Create the validation callback for a binding's pin.
    /// </summary>
    /// <param name="serverPin">The pin, as written in the binding.</param>
    /// <returns>A callback that accepts exactly the certificate holding the pinned key.</returns>
    /// <exception cref="FormatException">The pin is not a valid SHA-256 pin.</exception>
    public static RemoteCertificateValidationCallback CreateCallback(string serverPin)
    {
        var pin = CertificatePin.Parse(serverPin);
        return (_, certificate, _, _) => IsTrusted(pin, certificate);
    }

    /// <summary>
    /// Decide whether a presented certificate holds the pinned key.
    /// </summary>
    /// <param name="pin">The pin.</param>
    /// <param name="certificate">The presented certificate.</param>
    /// <returns>True only when the certificate's public key matches the pin.</returns>
    public static bool IsTrusted(CertificatePin pin, X509Certificate? certificate)
    {
        if (certificate is null)
        {
            return false;
        }

        try
        {
            if (certificate is X509Certificate2 certificate2)
            {
                return pin.Matches(certificate2);
            }

            using var loaded = X509CertificateLoader.LoadCertificate(certificate.GetRawCertData());
            return pin.Matches(loaded);
        }
        catch (Exception exception) when (exception is CryptographicException or ArgumentException)
        {
            return false;
        }
    }
}
