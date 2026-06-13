// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.Clustering;

/// <summary>
/// Event used by the fixture to warm up and verify the full cross-silo pipeline before tests run.
/// </summary>
/// <param name="Value">An arbitrary value.</param>
[EventType]
public record ClusterWarmedUp(int Value);
