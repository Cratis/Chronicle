// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Recommendations;

namespace Cratis.Chronicle.Projections;

public partial class ProjectionsManager
{
    static List<ProjectionDefinition> StampWithVersion(IEnumerable<ProjectionDefinition> definitions, string? version) =>
        [.. definitions.Select(definition => string.IsNullOrWhiteSpace(version) ? definition : definition with { Version = version })];

    /// <summary>
    /// Whether this exact registration just completed, with nothing since that it would have to react to.
    /// </summary>
    /// <param name="signature">The signature of the incoming registration.</param>
    /// <returns>True when it can be answered without doing the work again.</returns>
    async Task<bool> IsRepeatOfRecentRegistration(string signature)
    {
        if (_lastRegistrationSignature != signature ||
            _registrationNamespaces is null ||
            !_pendingRevival.IsEmpty ||
            _clock.GetUtcNow() - _lastRegistrationCompletedAt > IdenticalRegistrationWindow)
        {
            return false;
        }

        // A namespace added since is work the registration owes: its projections need subscribing there.
        var namespaces = await GrainFactory.GetGrain<INamespaces>(_eventStoreName).GetAll();
        return _registrationNamespaces.SetEquals(namespaces);
    }

    /// <summary>
    /// Announces the definitions a registration is about to replace. Namespace passes interleave with a registration
    /// and read the stored definitions, which are only replaced once the incoming ones are set and subscribed, so an
    /// announced definition keeps such a pass from setting or subscribing what is being replaced.
    /// </summary>
    /// <param name="definitions">The incoming definitions.</param>
    void Announce(IEnumerable<ProjectionDefinition> definitions)
    {
        foreach (var definition in definitions)
        {
            _registeringDefinitions[definition.Identifier] = definition;
        }
    }

    /// <summary>
    /// Withdraws definitions from the announcement.
    /// </summary>
    /// <param name="definitions">The definitions that no longer take part in the registration.</param>
    void Withdraw(IEnumerable<ProjectionDefinition> definitions)
    {
        foreach (var definition in definitions)
        {
            _registeringDefinitions.Remove(definition.Identifier);
        }
    }

    /// <summary>
    /// Keeps only definitions that are actually being replaced announced. An unchanged re-registration compares
    /// unequal by instance, so leaving it announced would keep it dark.
    /// </summary>
    /// <param name="changed">The definitions that are being replaced.</param>
    void KeepAnnounced(IEnumerable<ProjectionDefinition> changed)
    {
        var keep = changed.Select(definition => definition.Identifier).ToHashSet();
        foreach (var identifier in _registeringDefinitions.Keys.Where(identifier => !keep.Contains(identifier)).ToArray())
        {
            _registeringDefinitions.Remove(identifier);
        }
    }

    /// <summary>
    /// Ends the announcement. A pass that skipped a definition because it was announced must not leave it
    /// unsubscribed when the announcement ends, whatever way the registration ended.
    /// </summary>
    void EndAnnouncement()
    {
        _registeringDefinitions.Clear();
        if (!_skippedWhileRegistering) return;

        _skippedWhileRegistering = false;
        ScheduleResubscribeProjectionsThatAreNotSubscribed();
    }

    async Task IgnoreOlderRegistrations(IEnumerable<ProjectionDefinition> definitions)
    {
        foreach (var definition in definitions)
        {
            var stored = State.Projections.First(projection => projection.Identifier == definition.Identifier);

            // Registrations are stamped with their version, so an older one always carries it.
            logger.IgnoringOlderRegistration(definition.Identifier, definition.Version!, stored.Version!);
            await AddIgnoredRegistrationRecommendation(definition.Identifier, definition.Version!, stored.Version!);
        }
    }

    bool IsOlderThanStored(ProjectionDefinition definition, string? version) =>
        State.Projections.Any(stored => stored.Identifier == definition.Identifier && ProjectionVersions.IsOlder(version, stored.Version));

    /// <summary>
    /// Checks whether a full set is older than any stored projection it could retire. Another application sharing the
    /// event store registers its own versions, and those must not stop this application from retiring what it no
    /// longer registers.
    /// </summary>
    /// <param name="owner">The owner of the full set.</param>
    /// <param name="registrant">The registrant of the full set, if any.</param>
    /// <param name="version">The version of the full set, if any.</param>
    /// <returns>True if the full set is older than a projection it could retire.</returns>
    bool IsOlderThanAnyRetirableBy(ProjectionOwner owner, string? registrant, string? version) =>
        State.Projections.Any(stored =>
            stored.Owner == owner &&
            IsRegisteredBy(stored.Identifier, registrant) &&
            ProjectionVersions.IsOlder(version, stored.Version));

    async Task AddIgnoredRegistrationRecommendation(ProjectionId identifier, string incomingVersion, string storedVersion)
    {
        var namespaces = await GrainFactory.GetGrain<INamespaces>(_eventStoreName).GetAll();
        foreach (var @namespace in namespaces)
        {
            var recommendationsManager = GrainFactory.GetGrain<IRecommendationsManager>(0, new RecommendationsManagerKey(_eventStoreName, @namespace));

            // The manager deduplicates on the request, so the same projection and version pair is only ever listed once.
            await recommendationsManager.Add<IIgnoredProjectionRegistrationRecommendation, IgnoredProjectionRegistrationRequest>(
                $"Projection registration ignored: projection '{identifier}' was registered by an older client version {incomingVersion} than the stored {storedVersion}. Deploy the newer version, or set Cratis:Chronicle:SoftwareVersion (environment variable Cratis__Chronicle__SoftwareVersion) on the client to a version higher than the stored {storedVersion} to roll back deliberately.",
                new()
                {
                    ProjectionId = identifier,
                    IncomingVersion = incomingVersion,
                    StoredVersion = storedVersion
                });
        }
    }

    /// <summary>
    /// Records which registrant last registered each of the definitions, so a full set can later retire only what
    /// the same registrant registered. Several applications share one event store, each registering its own full set.
    /// </summary>
    /// <param name="definitions">The definitions in the registration.</param>
    /// <param name="registrant">The registrant, if the client identified itself.</param>
    /// <returns><see langword="true"/> if the recorded registrants changed and state needs writing.</returns>
    bool RecordRegistrant(IEnumerable<ProjectionDefinition> definitions, string? registrant)
    {
        if (string.IsNullOrEmpty(registrant)) return false;

        var changed = false;
        var registrants = new Dictionary<string, string>(State.Registrants);
        foreach (var definition in definitions)
        {
            if (registrants.TryGetValue(definition.Identifier.Value, out var current) && current == registrant) continue;
            registrants[definition.Identifier.Value] = registrant;
            changed = true;
        }

        if (changed) State.Registrants = registrants;
        return changed;
    }
}
