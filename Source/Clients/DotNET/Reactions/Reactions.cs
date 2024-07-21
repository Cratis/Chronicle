// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Reflection;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactions;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Identities;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactions;

/// <summary>
/// Represents an implementation of <see cref="IReactions"/>.
/// </summary>
public class Reactions : IReactions
{
    readonly IEventStore _eventStore;
    readonly IEventTypes _eventTypes;
    readonly IClientArtifactsProvider _clientArtifactsProvider;
    readonly IServiceProvider _serviceProvider;
    readonly IReactionMiddlewares _middlewares;
    readonly IEventSerializer _eventSerializer;
    readonly ICausationManager _causationManager;
    readonly ILogger<Reactions> _logger;
    readonly ILoggerFactory _loggerFactory;
    readonly IDictionary<Type, ReactionHandler> _handlers = new Dictionary<Type, ReactionHandler>();

    /// <summary>
    /// Initializes a new instance of the <see cref="Reactions"/> class.
    /// </summary>
    /// <param name="eventStore"><see cref="IEventStore"/> the reactions belong to.</param>
    /// <param name="eventTypes"><see cref="IEventTypes"/> for resolving event types.</param>
    /// <param name="clientArtifactsProvider"><see cref="IClientArtifactsProvider"/> for getting client artifacts.</param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> to get instances of types.</param>
    /// <param name="middlewares"><see cref="IReactionMiddlewares"/> to call.</param>
    /// <param name="eventSerializer"><see cref="IEventSerializer"/> for serializing of events.</param>
    /// <param name="causationManager"><see cref="ICausationManager"/> for working with causation.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    /// <param name="loggerFactory"><see cref="ILoggerFactory"/> for creating loggers.</param>
    public Reactions(
        IEventStore eventStore,
        IEventTypes eventTypes,
        IClientArtifactsProvider clientArtifactsProvider,
        IServiceProvider serviceProvider,
        IReactionMiddlewares middlewares,
        IEventSerializer eventSerializer,
        ICausationManager causationManager,
        ILogger<Reactions> logger,
        ILoggerFactory loggerFactory)
    {
        _eventStore = eventStore;
        _eventTypes = eventTypes;
        _clientArtifactsProvider = clientArtifactsProvider;
        _serviceProvider = serviceProvider;
        _middlewares = middlewares;
        _eventSerializer = eventSerializer;
        _causationManager = causationManager;
        _logger = logger;
        _loggerFactory = loggerFactory;
        eventStore.Connection.Lifecycle.OnConnected += Register;
    }

    /// <inheritdoc/>
    public Task Discover()
    {
        _logger.DiscoverAllReactions();
        var logger = _loggerFactory.CreateLogger<ReactionInvoker>();
        var handlers = _clientArtifactsProvider.Reactions
                            .ToDictionary(
                                _ => _,
                                reactionType =>
                                {
                                    var reaction = reactionType.GetCustomAttribute<ReactionAttribute>()!;
                                    return new ReactionHandler(
                                        reactionType.GetReactionId(),
                                        reactionType.GetEventSequenceId(),
                                        new ReactionInvoker(_serviceProvider, _eventStore.EventTypes, _middlewares, reactionType, logger),
                                        _causationManager);
                                });

        foreach (var handler in handlers)
        {
            _handlers.Add(handler);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Register()
    {
        foreach (var handler in _handlers.Values)
        {
            RegisterReaction(handler);
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public ReactionHandler GetHandlerById(ReactionId id)
    {
        var reactionHandler = _handlers.Values.SingleOrDefault(_ => _.Id == id);
        ThrowIfUnknownReactionId(reactionHandler, id);
        return reactionHandler!;
    }

    void ThrowIfUnknownReactionId(ReactionHandler? handler, ReactionId reactionId)
    {
        if (handler is null)
        {
            throw new UnknownReactionId(reactionId);
        }
    }

    void RegisterReaction(ReactionHandler handler)
    {
        _logger.RegisteringReaction(handler.Id);
        var registration = new RegisterReaction
        {
            ConnectionId = _eventStore.Connection.Lifecycle.ConnectionId,
            EventStoreName = _eventStore.EventStoreName,
            Namespace = _eventStore.Namespace,
            EventSequenceId = handler.EventSequenceId,
            ObserverId = handler.Id,
            EventTypes = handler.EventTypes.Select(_ => _.ToContract()).ToArray()
        };

#pragma warning disable CA2000 // Dispose objects before losing scope
        var messages = new BehaviorSubject<ReactionMessage>(new(new(registration)));
#pragma warning restore CA2000 // Dispose objects before losing scope
        var eventsToObserve = _eventStore.Connection.Services.Reactions.Observe(messages);
        eventsToObserve.Subscribe(
            events => ObserverMethod(messages, handler, events).Wait(),
            messages.Dispose);
    }

    async Task ObserverMethod(ISubject<ReactionMessage> messages, ReactionHandler handler, EventsToObserve events)
    {
        var lastSuccessfullyObservedEvent = EventSequenceNumber.Unavailable;
        var exceptionMessages = Enumerable.Empty<string>();
        var exceptionStackTrace = string.Empty;
        var state = ObservationState.Success;

        foreach (var @event in events.Events)
        {
            _logger.EventReceived(@event.Metadata.Type.Id, handler.Id);

            try
            {
                var context = @event.Context.ToClient();
                var metadata = @event.Metadata.ToClient();

                BaseIdentityProvider.SetCurrentIdentity(Identity.System with { OnBehalfOf = context.CausedBy });
                var eventType = _eventTypes.GetClrTypeFor(metadata.Type.Id);

                var content = await _eventSerializer.Deserialize(eventType, JsonNode.Parse(@event.Content)!.AsObject());

                await handler.OnNext(metadata, context, content);
                lastSuccessfullyObservedEvent = @event.Metadata.SequenceNumber;
            }
            catch (Exception ex)
            {
                exceptionMessages = ex.GetAllMessages();
                exceptionStackTrace = ex.StackTrace ?? string.Empty;
                state = ObservationState.Failed;
            }
        }

        var result = new ReactionResult
        {
            State = state,
            LastSuccessfulObservation = lastSuccessfullyObservedEvent,
            ExceptionMessages = exceptionMessages.ToList(),
            ExceptionStackTrace = exceptionStackTrace
        };
        messages.OnNext(new(new(result)));
    }
}
