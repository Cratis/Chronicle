// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.for_ReadModelReactors;

[EventType("34180000-0000-4000-8000-000000000004")]
public record ReducerWatchedFailure(int Value, string Marker);
