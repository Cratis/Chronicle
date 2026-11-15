// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Services.Observation.Reducers.for_Reducers.given;

public class a_paged_state_caching_observation_stream : a_state_caching_observation_stream
{
    protected override PageDelivery? PageDelivery => new() { Enabled = true };
}
