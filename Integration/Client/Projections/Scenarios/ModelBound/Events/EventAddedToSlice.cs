// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.Events;

[EventType]
public record EventAddedToSlice(Guid SliceId, Guid EventItemId, string Name);
