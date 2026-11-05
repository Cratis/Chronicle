// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Compliance.GDPR;

/// <summary>
/// Holds log messages for <see cref="PIIManager"/>.
/// </summary>
/// <remarks>
/// <para>
/// Erasing a subject and authorizing a new key for one are deliberate compliance acts rather than diagnostics, so
/// both are recorded at information level and both record their <b>completion</b> - an act that only logged its
/// intent would leave the most important question, whether it finished, to be inferred. The incomplete outcomes
/// are separate messages at error level, so a partial erasure can never read as a completed one.
/// </para>
/// <para>
/// The act is logged; the actor is not. Chronicle does not know who asked for it, and the log deliberately does
/// not name the data subject either - see <see cref="PIIManager"/> for why the subject appears as a binding.
/// </para>
/// </remarks>
internal static partial class PIIManagerLogMessages
{
    [LoggerMessage(LogLevel.Information, "Erased the encryption key for the subject bound to '{SubjectBinding}' in namespace '{Namespace}', across all {EventStoreCount} event stores in it: {EventStores}. Every PII value protected under that key is now permanently unreadable, and no event store in the namespace will provision or accept a key for the subject until a new one is explicitly authorized")]
    internal static partial void ErasedSubject(this ILogger<PIIManager> logger, string subjectBinding, EventStoreNamespaceName @namespace, int eventStoreCount, string eventStores);

    [LoggerMessage(LogLevel.Error, "Erasing the encryption key for the subject bound to '{SubjectBinding}' in namespace '{Namespace}' did NOT complete: {FailureCount} operation(s) failed across the {EventStoreCount} event stores {EventStores}. The subject is not erased - a key surviving in one event store is copied back into the others by the next forwarded event, so repeat the erasure once every event store is reachable")]
    internal static partial void SubjectErasureIncomplete(this ILogger<PIIManager> logger, string subjectBinding, EventStoreNamespaceName @namespace, int failureCount, int eventStoreCount, string eventStores);

    [LoggerMessage(LogLevel.Information, "Authorized a new encryption key for the erased subject bound to '{SubjectBinding}' in namespace '{Namespace}', across all {EventStoreCount} event stores in it: {EventStores}. The erased key does not come back - the next PII value written for the subject provisions a fresh, independent one that can decrypt nothing written before the erasure")]
    internal static partial void AllowedNewEncryptionKey(this ILogger<PIIManager> logger, string subjectBinding, EventStoreNamespaceName @namespace, int eventStoreCount, string eventStores);

    [LoggerMessage(LogLevel.Error, "Authorizing a new encryption key for the erased subject bound to '{SubjectBinding}' in namespace '{Namespace}' did NOT complete: {FailureCount} operation(s) failed across the {EventStoreCount} event stores {EventStores}. The event stores that were not reached still refuse to provision a key for the subject, so repeat the authorization once every one of them is reachable")]
    internal static partial void NewEncryptionKeyAuthorizationIncomplete(this ILogger<PIIManager> logger, string subjectBinding, EventStoreNamespaceName @namespace, int failureCount, int eventStoreCount, string eventStores);
}
