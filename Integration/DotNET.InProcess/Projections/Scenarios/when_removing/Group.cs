// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.ReadModels;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_removing;

public record Group(
    EventSourceId Id,
    string Name,
    IEnumerable<UserOnGroup> Users,
    EventSequenceNumber __lastHandledEventSequenceNumber);
