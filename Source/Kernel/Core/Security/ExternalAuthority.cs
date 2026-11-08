// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security;

/// <summary>
/// Constants identifying principals validated against the configured external authority.
/// </summary>
public static class ExternalAuthority
{
    /// <summary>
    /// The authentication type set on identities produced by validating an external-authority token.
    /// </summary>
    public const string AuthenticationType = "Chronicle.ExternalAuthority";

    /// <summary>
    /// The claim property holding the original JWT claim name; the default of "JsonWebTokenHandler.ShortClaimTypeProperty".
    /// </summary>
    public const string ShortClaimTypeProperty = "http://schemas.xmlsoap.org/ws/2005/05/identity/claimproperties/ShortTypeName";
}
