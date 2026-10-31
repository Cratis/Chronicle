// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security;

/// <summary>
/// Identifies claims issued by Chronicle's internal authority.
/// </summary>
public static class ChronicleClaimTypes
{
    /// <summary>
    /// Identifies a principal as a user rather than an application.
    /// </summary>
    public const string Principal = "chronicle_principal";

    /// <summary>
    /// The principal-kind value issued only for users.
    /// </summary>
    public const string User = "user";
}
