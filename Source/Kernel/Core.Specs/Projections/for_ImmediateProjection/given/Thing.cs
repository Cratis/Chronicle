// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias Client;

using Cratis.Arc.Queries.ModelBound;

namespace Cratis.Chronicle.Projections.for_ImmediateProjection.given;

[ReadModel]
[Client::Cratis.Chronicle.ReadModels.Passive]
[Client::Cratis.Chronicle.Projections.ModelBound.FromEvent<ThingConfigured>]
public record Thing(
    ThingId Id,
    [Client::Cratis.Chronicle.Projections.ModelBound.SetFrom<ThingConfigured>(nameof(ThingConfigured.Endpoint))] Endpoint? Endpoint = null,
    [Client::Cratis.Chronicle.Projections.ModelBound.SetFrom<ThingConfigured>(nameof(ThingConfigured.Name))] string Name = "",
    Enabled? Enabled = null);
