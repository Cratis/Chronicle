using System.Reflection;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Webhooks;

/// <summary>
/// Represents a converter for converting between webhook types.
/// </summary>
public static class WebhookDefinitionConverter
{
    public static Contracts.Observation.Webhooks.WebhookDefinition ToContract(this WebhookDefinition definition) =>
        new()
        {
            EventSequenceId = definition.EventSequenceId.Value,
            // TODO:
        };
    }
}
