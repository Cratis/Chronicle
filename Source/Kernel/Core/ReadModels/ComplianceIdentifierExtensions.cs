// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Extension methods for resolving the compliance subject identifier of a read-model document.
/// </summary>
public static class ComplianceIdentifierExtensions
{
    /// <summary>
    /// Resolves the compliance subject identifier for the read-model document an event projects into.
    /// </summary>
    /// <param name="context">The <see cref="EventContext"/> of the event being projected or reduced.</param>
    /// <param name="key">The resolved <see cref="Key"/> the read-model document is stored under.</param>
    /// <returns>The identifier to encrypt PII under and to stamp as the document's compliance subject.</returns>
    /// <remarks>
    /// A read-model document must encrypt and release its PII under a single, stable subject for its whole
    /// lifetime; otherwise the stored subject and the identity the PII was encrypted under diverge and the
    /// PII no longer decrypts on read. For a re-keyed projection or reducer the document key differs from the
    /// source event's event source id, and a single document can be fed by events from several source
    /// streams — so the per-event event source id is not a stable document identity. The resolved document
    /// key is, so it is used whenever the event's subject is simply its event source id. An explicit
    /// compliance subject (one that differs from the event source id) is honored as-is, preserving a
    /// caller-provided subject.
    /// </remarks>
    public static string ResolveComplianceIdentifier(this EventContext context, Key key) =>
        context.Subject?.IsSet == true && context.Subject.Value != context.EventSourceId.Value
            ? context.Subject.Value
            : key.Value?.ToString() ?? context.EventSourceId.Value;
}
