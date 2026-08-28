// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns;

/// <summary>
/// Defines a system that resolves which part of the day a moment belongs to.
/// </summary>
public interface ITimeBucketResolver
{
    /// <summary>
    /// Resolve the <see cref="TimeBucket"/> a moment belongs to.
    /// </summary>
    /// <param name="occurred">When it occurred.</param>
    /// <returns>The <see cref="TimeBucket"/>.</returns>
    TimeBucket Resolve(DateTimeOffset occurred);
}
