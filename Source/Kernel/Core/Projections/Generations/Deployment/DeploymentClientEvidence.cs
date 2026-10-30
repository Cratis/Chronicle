// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Projections.Generations.Deployment;

/// <summary>
/// Evidence for one currently connected client declaring the projection.
/// </summary>
/// <param name="ConnectionId">The declaring connection.</param>
/// <param name="RegisteredVersion">The version the client registered, without substituting its connection version.</param>
/// <param name="MatchesMinimumVersion">Whether the registered semantic version is at or above the minimum.</param>
/// <param name="Status">The reason the client does or does not satisfy the condition.</param>
public record DeploymentClientEvidence(ConnectionId ConnectionId, string? RegisteredVersion, bool MatchesMinimumVersion, DeploymentClientStatus Status);
