// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using Cratis.Monads;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Represents a null implementation of <see cref="IReactorInvoker"/> that does nothing.
/// </summary>
public class NullReactorInvoker : IReactorInvoker
{
    /// <inheritdoc/>
    public IImmutableList<EventType> EventTypes => ImmutableList<EventType>.Empty;

    /// <inheritdoc/>
    public Catch<ActivatedArtifact> CreateInstance(IServiceProvider serviceProvider) =>
        new ActivatedArtifact(new(), typeof(object), NullLoggerFactory.Instance);

    /// <inheritdoc/>
    public Task Invoke(object reactorInstance, object content, EventContext eventContext) => Task.CompletedTask;
}
