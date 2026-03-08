// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Monads;

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Represents a null implementation of <see cref="IReactorInvoker"/> that does nothing.
/// </summary>
public class NullReactorInvoker : IReactorInvoker
{
    static readonly Task<Catch> _successResult = Task.FromResult(Catch.Success());

    /// <inheritdoc/>
    public Task<Catch> Invoke(object content, EventContext eventContext) => _successResult;
}
