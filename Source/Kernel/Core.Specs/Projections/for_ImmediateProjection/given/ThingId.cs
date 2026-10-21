// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias Client;

namespace Cratis.Chronicle.Projections.for_ImmediateProjection.given;

public record ThingId(string Value) : Client::Cratis.Chronicle.Events.EventSourceId<string>(Value);
