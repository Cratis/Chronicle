// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Records removal of an administrator privilege.
/// </summary>
/// <param name="UserId">The user losing the privilege.</param>
/// <param name="RevokeReason">The reason the privilege was revoked.</param>
/// <param name="Proof">The kernel proof.</param>
[EventType]
public record UserRevokedAdministrator(UserId UserId, string RevokeReason, string Proof = "");
