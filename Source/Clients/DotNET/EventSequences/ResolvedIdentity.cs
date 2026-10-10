// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Represents current caused-by identity details, without subject-as-name fallback.
/// </summary>
/// <param name="Subject">The subject, empty if unresolved.</param>
/// <param name="Name">The display name, null unless resolved.</param>
/// <param name="UserName">The username, empty if unresolved.</param>
/// <param name="Resolution">The resolution status.</param>
/// <param name="OnBehalfOf">The next identity in the chain.</param>
public record ResolvedIdentity(string Subject, string? Name, string UserName, IdentityResolution Resolution, ResolvedIdentity? OnBehalfOf);
