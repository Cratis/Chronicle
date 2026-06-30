// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Test event for opening a ledger.
/// </summary>
/// <param name="Name">The ledger name.</param>
[EventType]
public record LedgerOpened(string Name);
