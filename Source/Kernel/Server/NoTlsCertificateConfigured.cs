// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server;

/// <summary>
/// The exception that is thrown when TLS is enabled but no certificate is configured in a production build.
/// </summary>
public class NoTlsCertificateConfigured()
    : Exception(
        "No TLS certificate is configured. The Chronicle port serves gRPC (HTTP/2) and the Workbench, " +
        "API and OAuth flows (HTTP/1.1) on a single TLS port, which requires a certificate. " +
        "Provide one through Tls:CertificatePath (and Tls:CertificatePassword) in configuration. " +
        "To run in cleartext instead — for example when TLS is terminated upstream by an ingress/reverse " +
        "proxy — set Tls:Enabled to false.");
