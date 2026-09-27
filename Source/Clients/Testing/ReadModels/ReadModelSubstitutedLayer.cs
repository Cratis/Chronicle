// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Represents a layer that <see cref="ReadModelScenario{TReadModel}"/> stands in for instead of running the
/// one a deployed Chronicle runs.
/// </summary>
/// <remarks>
/// Only the layers whose behavior depends on the shape of the read model appear here, because only those can
/// be told apart per scenario. The layers every in-process scenario substitutes regardless of shape — the
/// event context, storage and observer lifecycle, the read model definition metadata — are unconditional and
/// are described in the testing documentation rather than reported per read model.
/// </remarks>
public enum ReadModelSubstitutedLayer
{
    /// <summary>
    /// The read model sink. The harness materializes documents in memory and maps the document key onto the
    /// read model in C#, so nothing that depends on how the real sink stores a value is exercised.
    /// </summary>
    Sink = 0,

    /// <summary>
    /// <c language="csharp">[Join]</c> key resolution. The production engine resolves a join source against its real sink; the
    /// harness applies its own correction to reach the same root, so the resolution itself is not the one that
    /// runs live.
    /// </summary>
    JoinKeyResolution = 1,

    /// <summary>
    /// Deferred key handling. The harness retries root keys once and child keys to a fixed point after the
    /// seeded events; a deployed Chronicle defers the partition and redelivers when the parent arrives.
    /// </summary>
    DeferredKeyHandling = 2,

    /// <summary>
    /// Compliance and security. A deployed Chronicle encrypts the <c language="csharp">[PII]</c> and <c language="csharp">[Encrypted]</c> members
    /// of a read model on the way into the sink and releases them on the way out; the harness projects and reads
    /// plaintext, so a value that is unreadable, erased (for <c language="csharp">[PII]</c>) or wrongly-subjected in a running
    /// system still looks correct here. Named for its original, compliance-only scope; retained rather than
    /// renamed because this is a shipped public member.
    /// </summary>
    Compliance = 3
}
