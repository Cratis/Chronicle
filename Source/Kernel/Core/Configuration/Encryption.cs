// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents the encryption configuration within compliance settings.
/// </summary>
public class Encryption
{
    /// <summary>
    /// Gets the optional storage configuration for encryption keys.
    /// When not configured, the general <see cref="Storage"/> is used as the default.
    /// </summary>
    public Storage? Storage { get; init; }

    /// <summary>
    /// Gets whether the default storage backend keeps serving encryption keys alongside <see cref="Storage"/>
    /// while keys are migrated into it. Defaults to <see langword="false"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Turning a dedicated key store on without this is a one-way flip: the new store starts empty, every key
    /// already in the default storage becomes unreachable, and every value those keys protect reads back as an
    /// empty string — indistinguishable from a completed right-to-erasure, with nothing reporting it.
    /// </para>
    /// <para>
    /// With this set, both stores are live. A key is looked for in <see cref="Storage"/> first and, when it is
    /// only in the default storage, it is served from there and written into <see cref="Storage"/> as it is read.
    /// New keys are provisioned in <see cref="Storage"/> and mirrored to the default storage, so the two stay in
    /// step and the move can be reversed by turning <see cref="Storage"/> off again. There is no cutover window,
    /// no migration script and no verify pass; removing the keys from the default storage afterwards is an
    /// ordinary, separately decided cleanup.
    /// </para>
    /// <para>
    /// This has no effect unless <see cref="Storage"/> is configured — without a dedicated store there is nothing
    /// to migrate to, and the default storage serves the keys on its own.
    /// </para>
    /// </remarks>
    public bool MigrateFromDefaultStorage { get; init; }
}
