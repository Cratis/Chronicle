// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Services.Observation.Reactors.for_Reactors.given;

public class a_page_observation_stream : an_observation_stream
{
    protected override PageDelivery? PageDelivery => new() { Enabled = true };
}
