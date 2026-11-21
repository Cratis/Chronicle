// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// The outcome of asking the federation authority to issue a certificate.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FederationIssuanceOutcome
{
    /// <summary>
    /// A certificate was issued.
    /// </summary>
    Issued = 0,

    /// <summary>
    /// The request was refused. This is deliberately one outcome for an unknown, expired, used or revoked enrollment, an
    /// unknown or revoked certificate and a wrong secret, so a caller learns nothing about which.
    /// </summary>
    Rejected = 1,

    /// <summary>
    /// The enrollment was fine but the certificate request could not be honored, so the enrollment was not used up.
    /// </summary>
    InvalidRequest = 2
}
