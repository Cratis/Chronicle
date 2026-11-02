// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.ContainerProfile;

/// <summary>Private, per-run resource identities and ephemeral credentials. Never serialize.</summary>
/// <param name="Session">The session identity.</param>
/// <param name="RunId">The repetition identity.</param>
/// <param name="Directory">The run output directory.</param>
/// <param name="CertificatePassword">The ephemeral PFX password.</param>
/// <param name="ClientSecret">The ephemeral client credential.</param>
/// <param name="Sha">The source commit abbreviation.</param>
/// <param name="MongoImage">The MongoDB image reference.</param>
/// <param name="Trace">Whether this is a separate traced repetition.</param>
internal sealed record RunContext(string Session, string RunId, string Directory, string CertificatePassword, string ClientSecret, string Sha, string MongoImage, bool Trace)
{
    /// <summary>Gets the session network name.</summary>
    public string Network => $"chronicle-cp-{Session}";

    /// <summary>Gets the MongoDB container name.</summary>
    public string Mongo => $"{Network}-{RunId}-mongo";

    /// <summary>Gets the kernel container name.</summary>
    public string Kernel => $"{Network}-{RunId}-kernel";

    /// <summary>Gets the temporary certificate path.</summary>
    public string Certificate => Path.Combine(Directory, "chronicle.pfx");

    /// <summary>Gets the trace mount source.</summary>
    public string TraceDirectory => Path.Combine(Directory, "trace");

    /// <summary>Gets ownership and session labels.</summary>
    public string[] Labels => ["--label", "cratis.container-profile=1", "--label", $"cratis.container-profile.session={Session}"];
}
