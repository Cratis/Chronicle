// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans;

namespace Aksio.Cratis.Events.Store.Grains.Branching;

/// <summary>
/// Represents an implementation of <see cref="IBranch"/>.
/// </summary>
public class Branch : Grain, IBranch
{
    /// <inheritdoc/>
    public Task Merge() => Task.CompletedTask;
}
