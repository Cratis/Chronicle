// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Receivers;

/// <summary>
/// Restricts deliveries to the configured store and the namespace selected by the client resolver.
/// </summary>
/// <param name="client">The configured client.</param>
/// <param name="namespaceResolver">The configured namespace resolver.</param>
/// <remarks>
/// The client has no non-creating namespace lookup. Only the resolver-selected namespace is accepted;
/// a delivery cannot select or create another namespace. Receiver namespace names must start with a letter or
/// digit and contain only letters, digits, periods, underscores or hyphens.
/// </remarks>
internal sealed class DeliveryTargetValidation(IChronicleClient client, IEventStoreNamespaceResolver namespaceResolver)
{
    /// <summary>
    /// Validates a target and replaces request names with the configured names.
    /// </summary>
    /// <param name="target">The untrusted target.</param>
    /// <returns>The configured target, or null when the target is not allowed.</returns>
    internal DeliveryTarget? Validate(DeliveryTarget? target)
    {
        if (client.Options is not ChronicleClientOptions options)
        {
            return null;
        }

        var store = options.EventStore;
        if (target is null)
        {
            return null;
        }

        if (!IsConfiguredStore(target.EventStore, store))
        {
            return null;
        }

        var tenant = namespaceResolver.Resolve();
        if (tenant is null)
        {
            return null;
        }

        if (!IsValidNamespace(tenant.Value) || target.Namespace != tenant)
        {
            return null;
        }

        return target with { EventStore = store, Namespace = tenant };
    }

    static bool IsConfiguredStore(EventStoreName requested, EventStoreName configured) =>
        !string.IsNullOrWhiteSpace(configured.Value) && string.Equals(requested.Value, configured.Value, StringComparison.Ordinal);

    static bool IsValidNamespace(string name) =>
        !string.IsNullOrEmpty(name) && char.IsLetterOrDigit(name[0]) && name.All(_ => char.IsLetterOrDigit(_) || _ is '.' or '_' or '-');
}
