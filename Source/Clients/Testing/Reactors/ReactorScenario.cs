// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Cratis.Execution;

namespace Cratis.Chronicle.Testing.Reactors;

/// <summary>
/// Represents a lightweight, in-process scenario for testing <see cref="IReactor"/> implementations without any infrastructure.
/// </summary>
/// <remarks>
/// <para>
/// Activates a fresh instance of <typeparamref name="TReactor"/> from the provided (or default) service provider
/// and routes events directly through the <see cref="ReactorInvoker"/> — no Chronicle server, gRPC, or observer
/// registration required.
/// </para>
/// <para>
/// Usage:
/// <code>
/// var scenario = new ReactorScenario&lt;MyReactor&gt;(serviceProvider);
/// await scenario.Given.ForEventSource(myId).Events(new SomeEvent(), new AnotherEvent());
/// // Assert on side-effects captured by mocks in serviceProvider
/// </code>
/// </para>
/// </remarks>
/// <typeparam name="TReactor">The type of reactor under test.</typeparam>
/// <param name="serviceProvider">
/// Optional <see cref="IServiceProvider"/> for resolving the reactor and its dependencies.
/// When <see langword="null"/>, a <see cref="DefaultServiceProvider"/> is used which instantiates
/// the reactor via its default constructor.
/// </param>
public class ReactorScenario<TReactor>(IServiceProvider? serviceProvider = null)
    where TReactor : class, IReactor
{
    readonly IServiceProvider _serviceProvider = serviceProvider ?? new DefaultServiceProvider();
    readonly IEventTypes _eventTypes = Defaults.Instance.EventTypes;

    /// <summary>
    /// Gets the entry point of the fluent builder for providing events to the reactor.
    /// </summary>
    /// <remarks>
    /// Usage:
    /// <code>
    /// await scenario.Given
    ///     .ForEventSource(myId)
    ///     .Events(new SomeEvent(), new AnotherEvent());
    /// </code>
    /// </remarks>
    public ReactorScenarioGivenBuilder<TReactor> Given => new(this);

    /// <summary>
    /// Invokes the reactor with the provided events for a specific event source.
    /// </summary>
    /// <remarks>
    /// A fresh instance of <typeparamref name="TReactor"/> is activated from the service provider for
    /// each invocation, matching the production behavior where a new scope is created per event batch.
    /// </remarks>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> to associate with the events.</param>
    /// <param name="events">The event instances to invoke the reactor with, in order.</param>
    /// <returns>A <see cref="Task"/> that completes when all events have been handled.</returns>
    internal async Task InvokeWith(EventSourceId eventSourceId, IEnumerable<object> events)
    {
        using var loggerFactory = new NullLoggerFactory();
#pragma warning disable CA2000 // Dispose objects before losing scope
        var artifactActivator = new ClientArtifactsActivator(_serviceProvider, loggerFactory);
#pragma warning restore CA2000 // Dispose objects before losing scope
        var activationResult = artifactActivator.Activate(_serviceProvider, typeof(TReactor));
        if (activationResult.TryGetException(out var exception))
        {
            throw exception;
        }

        await using var activatedReactor = activationResult.AsT0;

        var invoker = new ReactorInvoker(
            _eventTypes,
#pragma warning disable CA2000 // Dispose objects before losing scope — ownership transfers to ReactorInvoker
            new ReactorMiddlewares([]),
#pragma warning restore CA2000
            typeof(TReactor),
            activatedReactor,
            NullLogger<ReactorInvoker>.Instance);

        foreach (var @event in events)
        {
            var eventType = _eventTypes.GetEventTypeFor(@event.GetType());
            var context = EventContext.From(
                "testing",
                "default",
                eventType,
                EventSourceType.Default,
                eventSourceId,
                EventStreamType.All,
                EventStreamId.Default,
                EventSequenceNumber.Unavailable,
                CorrelationId.New());

            await invoker.Invoke(@event, context);
        }
    }
}
