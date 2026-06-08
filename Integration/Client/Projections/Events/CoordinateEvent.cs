// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Geospatial;

namespace Cratis.Chronicle.Integration.Projections.Events;

[EventType]
public record CoordinateEvent(Coordinate Location);
