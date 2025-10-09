using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Webhooks;

/// <summary>
/// Represents a converter for converting between webhook types.
/// </summary>
public static class WebhookDefinitionConverter
{
    /// <summary>
    /// Converts a <see cref="Contracts.Observation.Webhooks.WebhookDefinition"/> to a <see cref="WebhookDefinition"/>.
    /// </summary>
    /// <param name="definition">The <see cref="WebhookDefinition"/> to convert.</param>
    /// <returns>The converted <see cref="Contracts.Observation.Webhooks.WebhookDefinition"/>.</returns>
    public static Contracts.Observation.Webhooks.WebhookDefinition ToContract(this WebhookDefinition definition) =>
        new()
        {
            EventSequenceId = definition.EventSequenceId.Value,
            EventTypes = definition.EventTypes.Select(_ => _.ToContract()).ToArray(),
            Identifier = definition.Identifier.Value,
            IsActive = definition.IsActive,
            IsReplayable = definition.IsReplayable,
            Target = definition.Target.ToContract()
        };

    static Contracts.Observation.Webhooks.WebhookTarget ToContract(this WebhookTarget target) =>
        new()
        {
            Url = target.Url.Value,
            Authentication = target.Authentication.ToContract(),
            BearerToken = target.BearerToken,
            Headers = target.Headers.ToDictionary(_ => _.Key, _ => _.Value),
            Password = target.Password,
            Username = target.Username
        };

    static Contracts.Observation.Webhooks.AuthenticationType ToContract(this AuthenticationType authentication) =>
        authentication switch
        {
            AuthenticationType.None => Contracts.Observation.Webhooks.AuthenticationType.None,
            AuthenticationType.Basic => Contracts.Observation.Webhooks.AuthenticationType.Basic,
            AuthenticationType.Bearer => Contracts.Observation.Webhooks.AuthenticationType.Bearer,
            _ => throw new ArgumentOutOfRangeException(nameof(authentication), authentication, null)
        };
}
