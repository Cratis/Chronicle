// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Security;

/// <summary>Validates explicitly configured principal and ordinary-store scopes at startup.</summary>
public class EventStoreAccessConfigurationValidation : IValidateOptions<ChronicleOptions>
{
    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string? name, ChronicleOptions options)
    {
        var external = options.Authentication.EventStoreAccess.ToArray();
        if (external.Any(entry => string.IsNullOrWhiteSpace(entry.Issuer) || string.IsNullOrWhiteSpace(entry.Subject)))
        {
            return ValidateOptionsResult.Fail("Event store access requires an exact issuer and subject.");
        }

        return external.Cast<ConfiguredEventStoreAccess>().Concat(options.Clients.SelectMany(client => client.EventStores)).All(IsValid)
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail("Event store access requires either an ordinary event store or AllEventStores, and valid access flags.");
    }

    static bool IsValid(ConfiguredEventStoreAccess entry) =>
        entry.AllEventStores == (entry.EventStore is null) &&
        (entry.EventStore is null || (!string.IsNullOrWhiteSpace(entry.EventStore) && !SystemStoreAccess.IsSystem(entry.EventStore))) &&
        (entry.Namespace is null || !string.IsNullOrWhiteSpace(entry.Namespace)) &&
        entry.Access != StoreAccess.None && (entry.Access & ~(StoreAccess.Read | StoreAccess.Append | StoreAccess.Manage)) == StoreAccess.None;
}
