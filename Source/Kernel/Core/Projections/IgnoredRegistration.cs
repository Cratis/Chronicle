// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// A projection registration that was ignored because the stored definition came from a newer client version.
/// </summary>
/// <param name="ProjectionId">The projection that was ignored.</param>
/// <param name="IncomingVersion">The version of the client that registered it.</param>
/// <param name="StoredVersion">The version the stored definition was registered with.</param>
internal record IgnoredRegistration(ProjectionId ProjectionId, string IncomingVersion, string StoredVersion);
