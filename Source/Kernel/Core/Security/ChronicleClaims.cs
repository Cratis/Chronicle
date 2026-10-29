// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security;

/// <summary>
/// Claims identifying principals issued by Chronicle.
/// </summary>
public static class ChronicleClaims
{
    /// <summary>
    /// The claim identifying the kind of Chronicle principal.
    /// </summary>
    public const string PrincipalKind = "chronicle_principal_kind";

    /// <summary>
    /// The principal kind for a signed-in Chronicle user.
    /// </summary>
    public const string User = "user";
}
