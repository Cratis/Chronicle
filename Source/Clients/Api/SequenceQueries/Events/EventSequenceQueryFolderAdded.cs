// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Api.SequenceQueries.Events;

/// <summary>
/// Event raised when a shared (system-owned) event sequence query folder is added.
/// </summary>
/// <param name="Name">The display name of the folder.</param>
[EventType]
public record EventSequenceQueryFolderAdded(string Name);
