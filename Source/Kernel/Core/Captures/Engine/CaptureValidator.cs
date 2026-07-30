// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Captures;
using Cratis.Chronicle.Concepts.Events;
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
        await ValidateAppends(eventStoreStorage, definition.Appends, messages);

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

    static async Task ValidateAppends(IEventStoreStorage eventStoreStorage, IReadOnlyList<AppendDefinition> appends, List<CaptureValidationMessage> messages)
    {
        foreach (var append in appends)
        {
            if (!await eventStoreStorage.EventTypes.HasFor(new EventTypeId(append.EventType)))
            {
                messages.Add(new($"There is no event type named '{append.EventType}'"));
            }

            if (append.When.Type == WhenClauseType.Expression)
            {
                messages.Add(new("Expression based when clauses are not supported by the capturing engine yet"));
            }

            messages.AddRange(append.FieldAssignments
                .Where(assignment => IsUnsupportedExpression(assignment.Value))
                .Select(assignment => new CaptureValidationMessage($"The expression '{assignment.Value}' is not supported by the capturing engine yet")));
        }
    }

    static bool IsUnsupportedExpression(string expression) =>
        (expression.StartsWith('$') && !expression.StartsWith("$.", StringComparison.Ordinal)) || expression.StartsWith('`');
}
