// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.given;

public static class administrator_surface_policy
{
    public static IReadOnlyDictionary<string, string> Required { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Cratis.Chronicle.Security.GrantAdministrator"] = WellKnownAuthorizationPolicies.Administrator,
        ["Cratis.Chronicle.Security.RevokeAdministrator"] = WellKnownAuthorizationPolicies.Administrator,
        ["Cratis.Chronicle.Security.Administrator"] = WellKnownAuthorizationPolicies.Administrator,
        ["Cratis.Chronicle.Security.AddUser"] = WellKnownAuthorizationPolicies.AdministratorOperations,
        ["Cratis.Chronicle.Security.RemoveUser"] = WellKnownAuthorizationPolicies.AdministratorOperations,
        ["Cratis.Chronicle.Security.RequirePasswordChange"] = WellKnownAuthorizationPolicies.AdministratorOperations,
        ["Cratis.Chronicle.Security.AddApplication"] = WellKnownAuthorizationPolicies.AdministratorOperations,
        ["Cratis.Chronicle.Security.RemoveApplication"] = WellKnownAuthorizationPolicies.AdministratorOperations,
        ["Cratis.Chronicle.Security.ChangeApplicationSecret"] = WellKnownAuthorizationPolicies.AdministratorOperations,
        ["Cratis.Chronicle.Jobs.StopJob"] = WellKnownAuthorizationPolicies.AdministratorOperations,
        ["Cratis.Chronicle.Jobs.StopJobWithResult"] = WellKnownAuthorizationPolicies.AdministratorOperations,
        ["Cratis.Chronicle.Jobs.ResumeJob"] = WellKnownAuthorizationPolicies.AdministratorOperations,
        ["Cratis.Chronicle.Jobs.ResumeJobWithResult"] = WellKnownAuthorizationPolicies.AdministratorOperations,
        ["Cratis.Chronicle.Jobs.DeleteJob"] = WellKnownAuthorizationPolicies.AdministratorOperations,
        ["Cratis.Chronicle.Observation.ReplayObserver"] = WellKnownAuthorizationPolicies.AdministratorOperations,
        ["Cratis.Chronicle.Observation.ReplayPartition"] = WellKnownAuthorizationPolicies.AdministratorOperations,
        ["Cratis.Chronicle.Observation.FastForward"] = WellKnownAuthorizationPolicies.AdministratorOperations,
        ["Cratis.Chronicle.Observation.RetryPartition"] = WellKnownAuthorizationPolicies.AdministratorOperations,
        ["Cratis.Chronicle.Observation.RetryFailedPartitions"] = WellKnownAuthorizationPolicies.AdministratorOperations,
        ["Cratis.Chronicle.Observation.ClearPartitionQuarantine"] = WellKnownAuthorizationPolicies.AdministratorOperations,
        ["Cratis.Chronicle.Observation.ClearObserverQuarantine"] = WellKnownAuthorizationPolicies.AdministratorOperations,
        ["Cratis.Chronicle.Observation.ClearFailedPartitions"] = WellKnownAuthorizationPolicies.AdministratorOperations,
        ["Cratis.Chronicle.DevelopmentTools.ResetKernelState"] = WellKnownAuthorizationPolicies.AdministratorOperations
    };

    public static IReadOnlyDictionary<string, string> ExemptCommands { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Cratis.Chronicle.Captures.ValidateCaptureDeclaration"] = "read-only declaration validation",
        ["Cratis.Chronicle.Occurrences.ClaimOccurrences"] = "existing occurrence worker coordination; not in the P3 administrator rollout",
        ["Cratis.Chronicle.Occurrences.CompleteOccurrence"] = "existing occurrence worker coordination; not in the P3 administrator rollout",
        ["Cratis.Chronicle.Occurrences.FailOccurrence"] = "existing occurrence worker coordination; not in the P3 administrator rollout",
        ["Cratis.Chronicle.Occurrences.RenewOccurrenceLease"] = "existing occurrence worker coordination; not in the P3 administrator rollout",
        ["Cratis.Chronicle.Occurrences.RetryQuarantinedOccurrence"] = "existing plain Authorize requirement is preserved; not in the P3 administrator rollout",
        ["Cratis.Chronicle.ProjectionEditor.GenerateDeclarativeCode"] = "read-only code generation",
        ["Cratis.Chronicle.ProjectionEditor.GenerateModelBoundCode"] = "read-only code generation",
        ["Cratis.Chronicle.ProjectionEditor.PreviewProjection"] = "read-only preview without persisted definitions",
        ["Cratis.Chronicle.Recommendations.PerformRecommendation"] = "existing recommendation workflow; not in the P3 administrator rollout",
        ["Cratis.Chronicle.Recommendations.IgnoreRecommendation"] = "existing recommendation workflow; not in the P3 administrator rollout",
        ["Cratis.Chronicle.Security.ChangeUserPassword"] = "self-service password change retains its existing checks",
        ["Cratis.Chronicle.Security.SetInitialAdminPassword"] = "anonymous bootstrap retains its existing checks",
        ["Cratis.Chronicle.Sequences.VerifyContent"] = "read-only content comparison"
    };

    public static IReadOnlyDictionary<string, string> ExemptReadModels { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Alerts.AlertIncidentSummary"] = "read-only incident diagnostics",
        ["Alerts.AlertIncidentPage"] = "read-only incident diagnostics",
        ["Alerts.AlertIncidentLookup"] = "read-only incident diagnostics",
        ["Captures.CapturedEvent"] = "read-only capture inspection",
        ["Captures.CaptureDetails"] = "read-only capture inspection",
        ["Clients.ConnectedClientDetails"] = "read-only client diagnostics",
        ["DevelopmentTools.DevelopmentToolsAvailability"] = "read-only feature availability",
        ["EventSources.EventSourceDetails"] = "read-only event-source inspection",
        ["EventStores.EventStoreNames"] = "read-only store discovery",
        ["EventTypes.EventTypeDetails"] = "read-only event-type inspection",
        ["ExternalServices.ExternalService"] = "read-only service definitions",
        ["Identities.IdentityDetails"] = "read-only identity inspection",
        ["Jobs.JobStepSummary"] = "read-only job diagnostics",
        ["Jobs.JobSummary"] = "read-only job diagnostics",
        ["Namespaces.NamespaceNames"] = "read-only namespace discovery",
        ["Observation.ObserverInformationForEventType"] = "read-only observer diagnostics",
        ["Observation.ObserverInformation"] = "read-only observer diagnostics",
        ["Observation.FailedPartitionDetails"] = "read-only observer diagnostics",
        ["Observation.Webhooks.WebhookDetails"] = "read-only webhook definitions",
        ["Occurrences.EffectReceiptRecord"] = "read-only occurrence diagnostics",
        ["Occurrences.OccurrenceSnapshot"] = "read-only occurrence diagnostics",
        ["Occurrences.LifecycleOccurrenceResult"] = "read-only occurrence diagnostics",
        ["Patterns.PatternScope"] = "read-only pattern inspection",
        ["Patterns.BehaviorPatternDetails"] = "read-only pattern inspection",
        ["ProjectionEditor.Projection"] = "read-only projection inspection",
        ["ProjectionEditor.ProjectionWithDeclaration"] = "read-only projection inspection",
        ["ReadModelDefinitions.ReadModelDefinition"] = "read-only model definitions",
        ["ReadModelExplorer.ReadModelOccurrence"] = "read-only model browsing",
        ["ReadModelExplorer.ReadModelInstance"] = "read-only model browsing",
        ["ReadModelExplorer.ReadModelSnapshot"] = "read-only model browsing",
        ["ReadModelLookups.ReadModelLookup"] = "read-only model lookup",
        ["Recommendations.RecommendationDetails"] = "read-only recommendation diagnostics",
        ["Schemas.TypeFormat"] = "read-only schema metadata",
        ["Security.Application"] = "AllApplications remains ungated by administrator policy",
        ["Security.User"] = "AllUsers remains ungated by administrator policy",
        ["Security.AdminPasswordStatus"] = "anonymous bootstrap status retains its existing checks",
        ["Seeding.SeedData"] = "read-only seed inspection",
        ["Seeding.EventSeedingSupport"] = "read-only feature availability",
        ["SequenceQueries.SequenceQuery"] = "read-only saved-query inspection",
        ["SequenceQueries.QueryFolder"] = "read-only saved-query inspection",
        ["Sequences.EventMetadata"] = "read-only event diagnostics",
        ["Sequences.StreamScopeCompletion"] = "read-only stream state",
        ["Sequences.SequenceHistogramBucket"] = "read-only event diagnostics",
        ["Sequences.EventSourceEvents"] = "read-only event browsing",
        ["Sequences.EventSequenceNames"] = "read-only sequence discovery",
        ["Sequences.ExportedEvent"] = "read-only event export",
        ["Sequences.ClosedStream"] = "read-only stream state",
        ["Sequences.EventSequenceTail"] = "read-only sequence state",
        ["Sequences.AppendedEvent"] = "read-only event browsing",
        ["Servers.ServerInstanceDetails"] = "read-only server diagnostics",
        ["Statistics.EventStatistics"] = "read-only event diagnostics"
    };
}
