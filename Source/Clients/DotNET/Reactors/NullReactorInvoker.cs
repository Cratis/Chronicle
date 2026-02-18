// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Represents a null implementation of <see cref="IReactorInvoker"/> that does nothing.
/// </summary>
public class NullReactorInvoker : IReactorInvoker
{
    /// <inheritdoc/>
    public IImmutableList<EventType> EventTypes => ImmutableList<EventType>.Empty;

    /// <inheritdoc/>
    public object CreateInstance(IServiceProvider serviceProvider) => new();

    /// <inheritdoc/>
    public Task Invoke(object reactorInstance, object content, EventContext eventContext) => Task.CompletedTask;
}
