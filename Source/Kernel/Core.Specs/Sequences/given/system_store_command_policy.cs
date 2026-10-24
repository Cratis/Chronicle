// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.given;

public static class system_store_command_policy
{
    const string JobRecoveryReason = "operational recovery of stuck System jobs (#5391); administrator policy in P3";
    const string SecurityReason = "legitimate path that writes users/applications into System; P2/P3";

    public static IReadOnlySet<string> Guarded { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "Cratis.Chronicle.Captures.DeleteCapture",
        "Cratis.Chronicle.Captures.SaveCapture",
        "Cratis.Chronicle.Captures.StartCapture",
        "Cratis.Chronicle.Captures.StopCapture",
        "Cratis.Chronicle.EventSources.RegisterEventSources",
        "Cratis.Chronicle.EventStores.EnsureEventStore",
        "Cratis.Chronicle.EventTypes.CreateEventType",
        "Cratis.Chronicle.EventTypes.RegisterEventTypes",
        "Cratis.Chronicle.EventTypes.RegisterSingleEventType",
        "Cratis.Chronicle.ExternalServices.AddExternalService",
        "Cratis.Chronicle.ExternalServices.AddExternalServices",
        "Cratis.Chronicle.ExternalServices.RemoveExternalService",
        "Cratis.Chronicle.ExternalServices.RemoveExternalServices",
        "Cratis.Chronicle.Identities.RenameIdentity",
        "Cratis.Chronicle.Namespaces.EnsureNamespace",
        "Cratis.Chronicle.Namespaces.RemoveNamespace",
        "Cratis.Chronicle.Observation.RemoveObserver",
        "Cratis.Chronicle.Observation.Webhooks.AddWebhook",
        "Cratis.Chronicle.Observation.Webhooks.AddWebhooks",
        "Cratis.Chronicle.Observation.Webhooks.RemoveWebhook",
        "Cratis.Chronicle.Observation.Webhooks.RemoveWebhooks",
        "Cratis.Chronicle.Occurrences.RecordEffectReceipt",
        "Cratis.Chronicle.Occurrences.RecordLifecycleOccurrence",
        "Cratis.Chronicle.Occurrences.RecordOccurrence",
        "Cratis.Chronicle.Occurrences.RegisterTriggers",
        "Cratis.Chronicle.ProjectionEditor.SaveProjection",
        "Cratis.Chronicle.ProjectionEditor.SaveProjectionWithInferredReadModel",
        "Cratis.Chronicle.ReadModelDefinitions.CreateReadModel",
        "Cratis.Chronicle.ReadModelDefinitions.UpdateReadModelDefinition",
        "Cratis.Chronicle.Recommendations.PerformRecommendation",
        "Cratis.Chronicle.Seeding.AddSeedEntry",
        "Cratis.Chronicle.Seeding.SeedEvents",
        "Cratis.Chronicle.SequenceQueries.DeleteSequenceQuery",
        "Cratis.Chronicle.SequenceQueries.DeleteSequenceQueryFolder",
        "Cratis.Chronicle.SequenceQueries.SaveSequenceQuery",
        "Cratis.Chronicle.SequenceQueries.SaveSequenceQueryFolder",
        "Cratis.Chronicle.Sequences.Append",
        "Cratis.Chronicle.Sequences.AppendMany",
        "Cratis.Chronicle.Sequences.AppendManyForEventSources",
        "Cratis.Chronicle.Sequences.AppendManyForEventSourcesWithNamedTags",
        "Cratis.Chronicle.Sequences.AppendManyOnce",
        "Cratis.Chronicle.Sequences.AppendManyOnceWithNamedTags",
        "Cratis.Chronicle.Sequences.AppendManyWithNamedTags",
        "Cratis.Chronicle.Sequences.AppendOnce",
        "Cratis.Chronicle.Sequences.AppendOnceWithNamedTags",
        "Cratis.Chronicle.Sequences.AppendWithNamedTags",
        "Cratis.Chronicle.Sequences.CompleteStream",
        "Cratis.Chronicle.Sequences.CompleteStreamScope",
        "Cratis.Chronicle.Sequences.Redact",
        "Cratis.Chronicle.Sequences.RedactForEventSource",
        "Cratis.Chronicle.Sequences.ReopenStreamScope",
        "Cratis.Chronicle.Sequences.Revise"
    };

    public static IReadOnlyDictionary<string, string> Allowed { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Cratis.Chronicle.Captures.ValidateCaptureDeclaration"] = "read-only declaration compilation and validation",
        ["Cratis.Chronicle.DevelopmentTools.ResetKernelState"] = "explicit development-only kernel reset without a store selector",
        ["Cratis.Chronicle.Jobs.DeleteJob"] = JobRecoveryReason,
        ["Cratis.Chronicle.Jobs.ResumeJob"] = JobRecoveryReason,
        ["Cratis.Chronicle.Jobs.ResumeJobWithResult"] = JobRecoveryReason,
        ["Cratis.Chronicle.Jobs.StopJob"] = JobRecoveryReason,
        ["Cratis.Chronicle.Jobs.StopJobWithResult"] = JobRecoveryReason,
        ["Cratis.Chronicle.Observation.ClearFailedPartitions"] = "operational clearing of existing failed partitions; administrator policy in P3",
        ["Cratis.Chronicle.Observation.ClearObserverQuarantine"] = "operational recovery of an existing quarantined observer; administrator policy in P3",
        ["Cratis.Chronicle.Observation.ClearPartitionQuarantine"] = "operational recovery of an existing quarantined partition; administrator policy in P3",
        ["Cratis.Chronicle.Observation.FastForward"] = "operational repositioning of an existing observer; administrator policy in P3",
        ["Cratis.Chronicle.Observation.ReplayObserver"] = "operational replay of an existing observer; administrator policy in P3",
        ["Cratis.Chronicle.Observation.ReplayPartition"] = "operational replay of an existing partition; administrator policy in P3",
        ["Cratis.Chronicle.Observation.RetryFailedPartitions"] = "operational retry of existing failed partitions; administrator policy in P3",
        ["Cratis.Chronicle.Observation.RetryPartition"] = "operational retry of an existing failed partition; administrator policy in P3",
        ["Cratis.Chronicle.Occurrences.ClaimOccurrences"] = "operational claim of existing queued occurrences, no event or definition write; administrator policy in P3",
        ["Cratis.Chronicle.Occurrences.CompleteOccurrence"] = "operational completion of an existing lease, no event or definition write; administrator policy in P3",
        ["Cratis.Chronicle.Occurrences.FailOccurrence"] = "operational failure of an existing lease, no event or definition write; administrator policy in P3",
        ["Cratis.Chronicle.Occurrences.RenewOccurrenceLease"] = "operational renewal of an existing lease, no event or definition write; administrator policy in P3",
        ["Cratis.Chronicle.Occurrences.RetryQuarantinedOccurrence"] = "operational retry of an existing quarantined occurrence, no event or definition write; administrator policy in P3",
        ["Cratis.Chronicle.ProjectionEditor.GenerateDeclarativeCode"] = "read-only declarative code generation",
        ["Cratis.Chronicle.ProjectionEditor.GenerateModelBoundCode"] = "read-only model-bound code generation",
        ["Cratis.Chronicle.ProjectionEditor.PreviewProjection"] = "read-only preview; preview definitions are not persisted",
        ["Cratis.Chronicle.Recommendations.IgnoreRecommendation"] = "operational dismissal of an existing recommendation, no event or definition write; administrator policy in P3",
        ["Cratis.Chronicle.Security.AddApplication"] = SecurityReason,
        ["Cratis.Chronicle.Security.AddUser"] = SecurityReason,
        ["Cratis.Chronicle.Security.ChangeApplicationSecret"] = SecurityReason,
        ["Cratis.Chronicle.Security.ChangeUserPassword"] = SecurityReason,
        ["Cratis.Chronicle.Security.RemoveApplication"] = SecurityReason,
        ["Cratis.Chronicle.Security.RemoveUser"] = SecurityReason,
        ["Cratis.Chronicle.Security.RequirePasswordChange"] = SecurityReason,
        ["Cratis.Chronicle.Security.SetInitialAdminPassword"] = SecurityReason,
        ["Cratis.Chronicle.Sequences.VerifyContent"] = "read-only stored-content comparison"
    };
}
