// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// The reasons a federation certificate is issued.
/// </summary>
public static class FederationIssuanceReasons
{
    /// <summary>
    /// The certificate was issued by redeeming an enrollment.
    /// </summary>
    public const string Enrollment = "enrollment";

    /// <summary>
    /// The certificate replaces one that was about to expire.
    /// </summary>
    public const string Renewal = "renewal";
}
