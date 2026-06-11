// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Represents a null implementation of <see cref="IReactorInvoker"/> that does nothing.
/// </summary>
public class NullReactorInvoker : IReactorInvoker
{
    static readonly Task<ReactorInvocationResult> _successResult = Task.FromResult(ReactorInvocationResult.Success());

    /// <inheritdoc/>
    public Task<ReactorInvocationResult> Invoke(object content, EventContext eventContext) => _successResult;
}
