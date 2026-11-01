// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.System;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Orders connected-client registrations on different event sequences without retaining ownership state.
/// </summary>
internal static class ObserverRegistrationPrecedence
{
    /// <summary>
    /// The .NET client type, matching Cratis.Chronicle.Connections.ChronicleClientIdentity.Type.
    /// </summary>
    const string DotNetClientType = ".NET";

    /// <summary>
    /// Compare a registration with the active owner of the observer ID on another event sequence.
    /// </summary>
    /// <param name="incoming">The registering client.</param>
    /// <param name="owning">The active subscription on another event sequence, if any.</param>
    /// <returns>The registration precedence.</returns>
    internal static RegistrationPrecedence Evaluate(ConnectedClient incoming, ObserverSubscription? owning)
    {
        if (owning?.IsServedByConnectedClient != true)
        {
            return new(RegistrationPrecedenceOutcome.NoConflict);
        }

        var owner = owning.Targets
            .Select(target => KnownVersion(target.ConnectedClient))
            .OfType<SemanticVersion>()
            .OrderDescending()
            .FirstOrDefault();
        var version = KnownVersion(incoming);
        var outcome = RegistrationPrecedenceOutcome.Unordered;
        if (version is not null && owner is not null)
        {
            var comparison = version.CompareTo(owner);
            outcome = comparison switch
            {
                > 0 => RegistrationPrecedenceOutcome.IncomingIsNewer,
                < 0 => RegistrationPrecedenceOutcome.OwnerIsNewer,
                _ => RegistrationPrecedenceOutcome.Unordered
            };
        }

        var ownerVersion = owner?.ToString() ?? owning.Targets.First(target => target.ConnectedClient is not null).ConnectedClient!.Version;
        return new(outcome, owning.ObserverKey.EventSequenceId, ownerVersion);
    }

    static SemanticVersion? KnownVersion(ConnectedClient? client)
    {
        if (client?.ClientType != DotNetClientType ||
            !SemanticVersion.TryParse(client.Version, out var version) || version is null ||
            (version.Major == 0 && version.Minor == 0 && version.Patch == 0))
        {
            return null;
        }

        return version;
    }
}
