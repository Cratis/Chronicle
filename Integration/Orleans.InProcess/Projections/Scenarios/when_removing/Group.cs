// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Scenarios.Models;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Scenarios.when_removing;

public record Group(
    EventSourceId Id,
    string Name,
    IEnumerable<UserOnGroup> Users);
