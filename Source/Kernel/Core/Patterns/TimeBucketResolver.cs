// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Patterns;

/// <summary>
/// Represents an implementation of <see cref="ITimeBucketResolver"/>.
/// </summary>
/// <remarks>
/// The rule itself lives on the concept, as <see cref="TimeBucketExtensions.ToTimeBucket"/>, because a caller
/// asking what usually happens at a given moment has to bucket that moment the same way the mining did. This
/// resolver exists so the engine can take it as a dependency and substitute it.
/// </remarks>
[Singleton]
public class TimeBucketResolver : ITimeBucketResolver
{
    /// <inheritdoc/>
    public TimeBucket Resolve(DateTimeOffset occurred) => occurred.ToTimeBucket();
}
