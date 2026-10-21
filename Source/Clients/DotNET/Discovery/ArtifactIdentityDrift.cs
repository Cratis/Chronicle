// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Discovery;

/// <summary>
/// Represents evidence of a likely artifact rename or move, not proof of one.
/// </summary>
/// <param name="Kind">The kind of artifact.</param>
/// <param name="PreviousId">The identifier found in the kernel.</param>
/// <param name="CurrentId">The newly declared identifier.</param>
public record ArtifactIdentityDrift(ArtifactKind Kind, string PreviousId, string CurrentId);
