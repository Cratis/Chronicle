// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Server;

/// <summary>
/// Loads the certificate for, and decides whether to open, the dedicated federation listener.
/// </summary>
internal static class FederationListener
{
    /// <summary>
    /// Loads the federation certificate, or returns null when federation is disabled.
    /// </summary>
    /// <param name="options">The Chronicle options.</param>
    /// <returns>The certificate, or null when federation is disabled.</returns>
    /// <exception cref="InvalidOperationException">Thrown when federation is enabled without a usable certificate.</exception>
    internal static X509Certificate2? LoadCertificate(ChronicleOptions options)
    {
        var federation = options.Federation;
        if (!federation.Enabled)
        {
            return null;
        }

        if (string.IsNullOrEmpty(federation.CertificatePath) || !File.Exists(federation.CertificatePath))
        {
            throw new InvalidOperationException(
                "Federation is enabled but no certificate was found. Set Federation:CertificatePath to a PKCS#12 file. " +
                "The federation listener never falls back to the main TLS certificate.");
        }

        var password = !string.IsNullOrEmpty(federation.CertificatePasswordPath) && File.Exists(federation.CertificatePasswordPath)
            ? File.ReadAllText(federation.CertificatePasswordPath).TrimEnd('\r', '\n')
            : null;

        return X509CertificateLoader.LoadPkcs12FromFile(federation.CertificatePath, password);
    }

    /// <summary>
    /// Registers the federation listener when federation is enabled.
    /// </summary>
    /// <param name="options">The Chronicle options.</param>
    /// <param name="certificate">The certificate returned by <see cref="LoadCertificate"/>.</param>
    /// <param name="listen">Registers a federation listener on a port with a certificate.</param>
    /// <exception cref="InvalidOperationException">Thrown when federation is enabled without a certificate, or its port collides with another listener.</exception>
    internal static void Configure(ChronicleOptions options, X509Certificate2? certificate, Action<int, X509Certificate2> listen)
    {
        if (!options.Federation.Enabled)
        {
            return;
        }

        if (certificate is null)
        {
            throw new InvalidOperationException("Federation is enabled but no certificate is available.");
        }

        if (options.Federation.Port == options.Port || options.Federation.Port == options.DedicatedHealthPort)
        {
            throw new InvalidOperationException(
                $"Federation:Port {options.Federation.Port} collides with another Chronicle listener. Federation is served on its own port.");
        }

        listen(options.Federation.Port, certificate);
    }
}
