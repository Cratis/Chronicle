// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.Specifications.Projections.Scenarios.ReadModels;

public record Group(
    EventSourceId Id,
    string Name,
    IEnumerable<UserOnGroup> Users,
    EventSequenceNumber __lastHandledEventSequenceNumber = default!);
