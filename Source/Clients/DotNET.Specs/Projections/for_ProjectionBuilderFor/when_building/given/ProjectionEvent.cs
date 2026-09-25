// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections.for_ProjectionBuilderFor.when_building.given;

[EventType]
public record ProjectionEvent(string Name);
