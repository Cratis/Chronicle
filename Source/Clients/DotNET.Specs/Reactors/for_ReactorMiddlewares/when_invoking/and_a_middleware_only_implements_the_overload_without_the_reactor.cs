// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors.for_ReactorMiddlewares.when_invoking;

/// <summary>
/// The overload carrying the reactor was added alongside the original one, so a middleware written before it
/// existed still has to be called. The default interface implementation is what makes that hold.
/// </summary>
public class and_a_middleware_only_implements_the_overload_without_the_reactor : Specification
{
    MiddlewareWithoutReactor _middleware;
    ReactorMiddlewares _middlewares;

    void Establish()
    {
        _middleware = new MiddlewareWithoutReactor();
        _middlewares = new([new ActivatedArtifact<IReactorMiddleware>(_middleware, Substitute.For<ILogger<ActivatedArtifact>>())]);
    }

    async Task Because()
    {
        await _middlewares.BeforeInvoke("some-reactor", EventContext.Empty, new MyEvent());
        await _middlewares.AfterInvoke("some-reactor", EventContext.Empty, new MyEvent());
    }

    [Fact] void should_call_before_invoke() => _middleware.BeforeInvokeCalls.ShouldEqual(1);
    [Fact] void should_call_after_invoke() => _middleware.AfterInvokeCalls.ShouldEqual(1);

    class MiddlewareWithoutReactor : IReactorMiddleware
    {
        public int BeforeInvokeCalls { get; private set; }

        public int AfterInvokeCalls { get; private set; }

        public Task BeforeInvoke(EventContext eventContext, object @event)
        {
            BeforeInvokeCalls++;
            return Task.CompletedTask;
        }

        public Task AfterInvoke(EventContext eventContext, object @event)
        {
            AfterInvokeCalls++;
            return Task.CompletedTask;
        }
    }
}
