// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.ContainerProfile;

/// <summary>Ephemeral TLS and client credentials for one run. Never serialize or log.</summary>
/// <param name="CertificatePassword">The ephemeral PFX password.</param>
/// <param name="ClientSecret">The ephemeral client credential.</param>
internal sealed record RunCredentials(string CertificatePassword, string ClientSecret);
