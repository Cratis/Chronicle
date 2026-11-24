// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Kernel.Integration.NamespaceFencing.given;

/// <summary>Retains real SDK writer delegates without naming the SDK/Core IEventSequence homonyms.</summary>
/// <param name="Append">The captured writer's immediate append.</param>
/// <param name="AppendOnce">The captured writer's keyed append, preserving success and deduplication evidence.</param>
public sealed record CapturedWriter(
    Func<string, NamespaceFenceValue, Task<bool>> Append,
    Func<string, NamespaceFenceValue, string, Task<(bool IsSuccess, bool WasAlreadyPresent)>> AppendOnce);
