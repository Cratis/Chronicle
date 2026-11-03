// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Benchmarks.ClientStartup;

/// <summary>
/// Represents the event used to verify actual reducer dispatch.
/// </summary>
[EventType("client-startup-recorded")]
public record CatalogRecorded;
