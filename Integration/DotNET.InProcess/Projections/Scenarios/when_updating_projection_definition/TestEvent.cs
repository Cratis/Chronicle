// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_updating_projection_definition;

[EventType("96e4ef76-ff62-4e43-9b75-a61f5e8c1c40")]
public record TestEvent(string Name, string Description);
