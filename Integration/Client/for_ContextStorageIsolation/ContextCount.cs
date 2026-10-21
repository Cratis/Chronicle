// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_ContextStorageIsolation;

/// <summary>
/// Counts the events for one event source.
/// </summary>
/// <param name="Count">The number of recorded events.</param>
public record ContextCount(int Count);
