// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.ContainerProfile;

/// <summary>Private per-run ownership, paths and ephemeral credentials. Never serialize.</summary>
/// <param name="SessionInfo">The sweep's resource scope and source identity.</param>
/// <param name="Run">The repetition identity.</param>
/// <param name="Directory">The run output directory.</param>
/// <param name="Credentials">The ephemeral credentials.</param>
internal sealed record RunContext(ProfileSession SessionInfo, RunIdentity Run, string Directory, RunCredentials Credentials)
{
    /// <summary>Gets the session identity.</summary>
    public string Session => SessionInfo.Id;

    /// <summary>Gets the run identity.</summary>
    public string RunId => Run.Id;

    /// <summary>Gets the source commit abbreviation.</summary>
    public string Sha => SessionInfo.Sha;

    /// <summary>Gets the selected MongoDB image.</summary>
    public string MongoImage => SessionInfo.MongoImage;

    /// <summary>Gets whether this is a trace repetition.</summary>
    public bool Trace => Run.Trace;

    /// <summary>Gets the temporary certificate password.</summary>
    public string CertificatePassword => Credentials.CertificatePassword;

    /// <summary>Gets the temporary client credential.</summary>
    public string ClientSecret => Credentials.ClientSecret;

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
