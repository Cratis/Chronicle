// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Test event that opens an order.
/// </summary>
/// <param name="Reference">The order reference.</param>
[EventType]
public record NotedOrderOpened(string Reference);
