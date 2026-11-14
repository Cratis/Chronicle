// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// The result of redeeming an enrollment code on a kernel.
/// </summary>
/// <param name="Identity">The identity the kernel now holds, <c language="text">spiffe://&lt;trust-domain&gt;/customer/&lt;id&gt;/kernel/&lt;instance&gt;</c>.</param>
/// <param name="Serial">The serial number of its certificate.</param>
/// <param name="NotAfter">When the certificate expires. The kernel renews it automatically before then.</param>
/// <param name="Endpoint">The public endpoint of the authority.</param>
/// <param name="AlreadyEnrolled">True when the kernel was already enrolled with the same authority, so nothing was changed.</param>
public record FederationRedemption(string Identity, string Serial, DateTimeOffset NotAfter, string Endpoint, bool AlreadyEnrolled);
