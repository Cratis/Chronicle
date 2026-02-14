// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.Specifications.Projections.Scenarios.when_projecting_with_join;

public record User(EventSourceId Id, string Name, string ProfileName, EventSourceId GroupId, string GroupName, EventSequenceNumber __lastHandledEventSequenceNumber);
