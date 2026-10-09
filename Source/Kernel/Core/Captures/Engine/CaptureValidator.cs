// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Captures;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.ExternalServices;
using Cratis.Chronicle.Storage;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Captures.Engine;

/// <summary>
/// Represents an implementation of <see cref="ICaptureValidator"/>.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for resolving external services and event types.</param>
[Singleton]
public class CaptureValidator(IStorage storage) : ICaptureValidator
{
    /// <inheritdoc/>
    public async Task<IEnumerable<CaptureValidationMessage>> Validate(EventStoreName eventStore, CaptureDefinition definition)
    {
        var messages = new List<CaptureValidationMessage>();
        var eventStoreStorage = storage.GetEventStore(eventStore);

        await ValidateSource(eventStoreStorage, definition.Source, messages);
        ValidateScopes(definition, messages);
        await ValidateAppends(eventStoreStorage, definition, messages);

        return messages;
    }

    static void ValidateScopes(CaptureDefinition definition, List<CaptureValidationMessage> messages)
    {
        if (definition.Map is not null)
        {
            messages.Add(new("Map operations are not supported by the capturing engine yet"));
        }

        if (definition.Nested.Count > 0)
        {
            messages.Add(new("Nested scopes are not supported by the capturing engine yet"));
        }

        if (definition.Children.Count > 0)
        {
            messages.Add(new("Children scopes are not supported by the capturing engine yet"));
        }

        if (definition.Appends.Count == 0)
        {
            messages.Add(new("A capture must define at least one append"));
        }
    }

    static async Task ValidateSource(IEventStoreStorage eventStoreStorage, SourceDefinition source, List<CaptureValidationMessage> messages)
    {
        if (source.Type == SourceType.Events)
        {
            await ValidateEventsSource(eventStoreStorage, source, messages);
            return;
        }

        if (source.Type != SourceType.Api)
        {
            messages.Add(new($"'{source.Type.ToString().ToLowerInvariant()}' sources are not supported by the capturing engine yet"));
            return;
        }

        if (string.IsNullOrWhiteSpace(source.Api))
        {
            messages.Add(new("An api source must reference an external service by name"));
        }
        else
        {
            var externalServices = await eventStoreStorage.ExternalServices.GetAll();
            var externalService = externalServices.FirstOrDefault(service => service.Name == new ExternalServiceName(source.Api));
            if (externalService is null)
            {
                messages.Add(new($"There is no external service named '{source.Api}'"));
            }
            else if (externalService.Endpoint.Type != ExternalServiceEndpointType.Http)
            {
                messages.Add(new($"The external service '{source.Api}' is not an HTTP service"));
            }
        }

        if (string.IsNullOrWhiteSpace(source.Poll))
        {
            messages.Add(new("An api source must define a poll interval, e.g. 'poll 5m'"));
        }
        else if (!CapturePollInterval.TryParse(source.Poll, out _))
        {
            messages.Add(new($"'{source.Poll}' is not a valid poll interval - use a number followed by s, m, h or d, e.g. '5m'"));
        }
    }

    static async Task ValidateEventsSource(IEventStoreStorage eventStoreStorage, SourceDefinition source, List<CaptureValidationMessage> messages)
    {
        var events = source.Events ?? [];
        if (events.Count == 0)
        {
            messages.Add(new("An events source must name at least one public event type to capture from, e.g. 'from ShipmentDispatched'"));
        }

        var schemas = new List<EventTypeSchema>();
        foreach (var eventType in events.Distinct())
        {
            if (await eventStoreStorage.EventTypes.HasFor(new EventTypeId(eventType)))
            {
                schemas.Add(await eventStoreStorage.EventTypes.GetFor(new EventTypeId(eventType)));
            }
            else
            {
                messages.Add(new($"There is no event type named '{eventType}' to capture from"));
            }
        }

        messages.AddRange(EventsCaptureSequence.Resolve(source.Sequence, schemas, out _).Select(error => new CaptureValidationMessage(error)));

        if (!string.IsNullOrWhiteSpace(source.Poll))
        {
            messages.Add(new("An events source is observed, not polled - remove the poll interval"));
        }
    }

    static async Task ValidateAppends(IEventStoreStorage eventStoreStorage, CaptureDefinition definition, List<CaptureValidationMessage> messages)
    {
        var sourceEvents = definition.Source.Type == SourceType.Events ? (definition.Source.Events ?? []) : [];
        foreach (var append in definition.Appends)
        {
            if (sourceEvents.Contains(append.EventType))
            {
                messages.Add(new($"'{append.EventType}' is a public event captured from the inbox and cannot also be appended - appended events must be private events of this event store"));
            }

            if (!await eventStoreStorage.EventTypes.HasFor(new EventTypeId(append.EventType)))
            {
                messages.Add(new($"There is no event type named '{append.EventType}'"));
            }
            else if (definition.Source.Type == SourceType.Events &&
                (await eventStoreStorage.EventTypes.GetFor(new EventTypeId(append.EventType))).Visibility == EventTypeVisibility.Public)
            {
                messages.Add(new($"'{append.EventType}' is a public event type - a capture appends private events of this event store, and appending a public event would publish it from the capture"));
            }

            if (append.When.Type == WhenClauseType.Expression)
            {
                messages.Add(new("Expression based when clauses are not supported by the capturing engine yet"));
            }

            messages.AddRange(append.FieldAssignments
                .Where(assignment => IsUnsupportedExpression(assignment.Value, definition.Source.Type))
                .Select(assignment => new CaptureValidationMessage($"The expression '{assignment.Value}' is not supported by the capturing engine yet")));
        }
    }

    static bool IsUnsupportedExpression(string expression, SourceType sourceType) =>
        (expression.StartsWith('$') && !expression.StartsWith("$.", StringComparison.Ordinal) && !IsEventContextExpression(expression, sourceType)) || expression.StartsWith('`');

    static bool IsEventContextExpression(string expression, SourceType sourceType) =>
        sourceType == SourceType.Events &&
        (expression == WellKnownExpressions.EventSourceId || expression.StartsWith("$context.", StringComparison.Ordinal));
}
