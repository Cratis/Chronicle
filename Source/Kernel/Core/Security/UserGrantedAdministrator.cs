// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Records a separately stored administrator privilege grant.
/// </summary>
/// <param name="UserId">The user receiving the privilege.</param>
/// <param name="Reason">The authority for the grant.</param>
/// <param name="Evidence">The evidence supporting the grant.</param>
/// <param name="Proof">The kernel proof.</param>
[EventType]
public record UserGrantedAdministrator(UserId UserId, GrantReason Reason, string Evidence, string Proof = "");
