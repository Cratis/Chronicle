// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Represents one direction of a federation relationship as the producer knows it.
/// </summary>
/// <param name="Id">The link identifier.</param>
/// <param name="Pin">The pinned client certificate of the peer.</param>
/// <param name="Grant">What the peer may read.</param>
/// <param name="Revoked">Whether the link has been revoked.</param>
public record FederationLink(string Id, CertificatePin Pin, FederationGrant Grant, bool Revoked = false);
