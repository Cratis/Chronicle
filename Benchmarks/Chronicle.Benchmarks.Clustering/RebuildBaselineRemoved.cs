// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>Removes a baseline root before a later re-creation.</summary>
[EventType]
public record RebuildBaselineRemoved;
