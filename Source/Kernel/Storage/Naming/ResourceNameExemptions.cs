// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Naming;

/// <summary>
/// Represents the names the kernel creates for itself, which the resource name rule does not apply to.
/// </summary>
/// <remarks>
/// Every entry has to be needed: an entry that the rule would accept anyway is a mistake and is refused.
/// </remarks>
public sealed class ResourceNameExemptions
{
    readonly HashSet<string> _eventStores = new(StringComparer.Ordinal);
    readonly HashSet<(string EventStore, string Namespace)> _namespaces = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceNameExemptions"/> class.
    /// </summary>
    /// <param name="exemptions">The exempt event stores, each with the namespaces that are exempt within it.</param>
    public ResourceNameExemptions(IEnumerable<(string EventStore, IEnumerable<string> Namespaces)>? exemptions = null)
    {
        foreach (var (eventStore, namespaces) in exemptions ?? [])
        {
            if (ResourceNames.ForEventStore(eventStore) == ResourceNameViolation.None)
            {
                throw new UnusedResourceNameExemption(eventStore);
            }

            _eventStores.Add(eventStore);
            foreach (var @namespace in namespaces)
            {
                if (ResourceNames.ForNamespace(@namespace) == ResourceNameViolation.None)
                {
                    throw new UnusedResourceNameExemption(@namespace);
                }

                _namespaces.Add((eventStore, @namespace));
            }
        }
    }

    /// <summary>
    /// Gets the instance without any exemptions.
    /// </summary>
    public static ResourceNameExemptions None { get; } = new();

    /// <summary>
    /// Check whether an event store is exempt.
    /// </summary>
    /// <param name="eventStore">The event store name.</param>
    /// <returns>True if exempt.</returns>
    public bool IsExempt(string eventStore) => _eventStores.Count > 0 && _eventStores.Contains(eventStore);

    /// <summary>
    /// Check whether a namespace of an event store is exempt.
    /// </summary>
    /// <param name="eventStore">The event store name.</param>
    /// <param name="namespace">The namespace name.</param>
    /// <returns>True if exempt.</returns>
    public bool IsExempt(string eventStore, string @namespace) => _namespaces.Count > 0 && _namespaces.Contains((eventStore, @namespace));
}
