// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Generations.Deployment;

/// <summary>
/// A deployment condition decision and the client evidence used to reach it.
/// </summary>
/// <param name="State">The condition outcome.</param>
/// <param name="Clients">Evidence for every declaring client.</param>
public record DeploymentConditionResult(DeploymentConditionState State, IEnumerable<DeploymentClientEvidence> Clients);
