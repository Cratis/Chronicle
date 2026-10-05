// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.for_Reducers.when_replaying;

/// <summary>
/// A number that was added.
/// </summary>
/// <param name="Number">The number.</param>
[EventType]
public record ReplayNumberAdded(int Number);
