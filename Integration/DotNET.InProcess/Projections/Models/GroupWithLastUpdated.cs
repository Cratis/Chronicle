// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.Models;

public record GroupWithLastUpdated(
    EventSourceId Id,
    string Name,
    IEnumerable<UserOnGroup> Users,
    DateTimeOffset LastUpdated,
    EventSequenceNumber __lastHandledEventSequenceNumber);
