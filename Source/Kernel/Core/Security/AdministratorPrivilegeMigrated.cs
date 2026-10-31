// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Records the outcome of the one-time administrator privilege migration.
/// </summary>
/// <param name="Outcome">Whether bootstrap provenance was proven.</param>
/// <param name="UserId">The proven user, when any.</param>
/// <param name="Proof">The kernel proof.</param>
[EventType]
public record AdministratorPrivilegeMigrated(string Outcome, UserId? UserId, string Proof = "");
