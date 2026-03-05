// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_seeded_events;

public record ItemsReadModel(int TotalCount, EventSequenceNumber? __lastHandledEventSequenceNumber = default);
