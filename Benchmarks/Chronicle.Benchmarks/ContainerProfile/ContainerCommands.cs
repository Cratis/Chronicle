// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Chronicle.Benchmarks.ContainerProfile;

/// <summary>Builds labelled setup commands for both planning and execution.</summary>
internal static class ContainerCommands
{
    /// <summary>Creates the private session network.</summary>
    /// <param name="context">The run context.</param>
    /// <returns>The bounded command.</returns>
    public static DockerCommand Network(RunContext context) => new("create network", ["network", "create", .. context.Labels, context.Network], TimeSpan.FromSeconds(30));

    /// <summary>Creates a fresh, unconstrained MongoDB replica-set member.</summary>
    /// <param name="context">The run context.</param>
    /// <returns>The bounded command.</returns>
    public static DockerCommand Mongo(RunContext context) => new("create MongoDB", ["create", "--name", context.Mongo, "--network", context.Network, "--network-alias", "mongo", .. context.Labels, context.MongoImage, "--replSet", "rs0", "--bind_ip_all"], TimeSpan.FromSeconds(60));

    /// <summary>Initializes the replica set.</summary>
    /// <param name="context">The run context.</param>
    /// <returns>The bounded command.</returns>
    public static DockerCommand Initiate(RunContext context) => new("initiate replica set", ["exec", context.Mongo, "mongosh", "--quiet", "--eval", "rs.initiate({_id:'rs0',members:[{_id:0,host:'mongo:27017'}]})"], TimeSpan.FromSeconds(30));

    /// <summary>Checks whether MongoDB is writable.</summary>
    /// <param name="context">The run context.</param>
    /// <returns>The bounded command.</returns>
    public static DockerCommand Primary(RunContext context) => new("wait for primary", ["exec", context.Mongo, "mongosh", "--quiet", "--eval", "if (!db.hello().isWritablePrimary) quit(1)"], TimeSpan.FromSeconds(30));

    /// <summary>Starts an existing container.</summary>
    /// <param name="container">The container identity.</param>
    /// <returns>The bounded command.</returns>
    public static DockerCommand Start(string container) => new("start container", ["start", container], TimeSpan.FromSeconds(60));

    /// <summary>Creates a kernel with explicit resource and GC settings.</summary>
    /// <param name="context">The run context, containing private credentials.</param>
    /// <param name="cell">The matrix configuration.</param>
    /// <returns>The bounded command; do not log real credential arguments.</returns>
    public static DockerCommand Kernel(RunContext context, MatrixCell cell)
    {
        var arguments = new List<string>
        {
            "create", "--name", context.Kernel, "--network", context.Network,
            "--cpus", cell.Cpus.ToString(CultureInfo.InvariantCulture), "--memory", cell.Memory, "--memory-swap", cell.Memory,
            "--publish", "127.0.0.1::35000", "--publish", "127.0.0.1::8080",
            "--mount", $"type=bind,source={context.Certificate},target=/app/certs/chronicle.pfx,readonly"
        };
        arguments.AddRange(context.Labels);
        AddEnvironment(arguments, KernelEnvironment(context).Concat(cell.Gc.Environment));
        if (context.Trace)
        {
            arguments.AddRange(["--mount", $"type=bind,source={context.TraceDirectory},target=/trace"]);
            AddEnvironment(arguments, ["DOTNET_EnableEventPipe=1", "DOTNET_EventPipeOutputPath=/trace/kernel.nettrace", "DOTNET_EventPipeConfig=Microsoft-Windows-DotNETRuntime:0x1:4"]);
        }
        arguments.Add(cell.Image.Reference(context.Sha));
        return new DockerCommand("create kernel", arguments, TimeSpan.FromSeconds(60));
    }

    static string[] KernelEnvironment(RunContext context) =>
    [
        "Cratis__Chronicle__Storage__ConnectionDetails=mongodb://mongo:27017/?replicaSet=rs0",
        "Cratis__Chronicle__Tls__CertificatePath=/app/certs/chronicle.pfx",
        $"Cratis__Chronicle__Tls__CertificatePassword={context.CertificatePassword}",
        "Cratis__Chronicle__EncryptionCertificate__CertificatePath=/app/certs/chronicle.pfx",
        $"Cratis__Chronicle__EncryptionCertificate__CertificatePassword={context.CertificatePassword}",
        "Cratis__Chronicle__Clients__0__ClientId=container-profile",
        $"Cratis__Chronicle__Clients__0__ClientSecret={context.ClientSecret}",
        "Cratis__Chronicle__Health__Port=8080",
        "Cratis__Chronicle__Health__Tls=false"
    ];

    static void AddEnvironment(List<string> arguments, IEnumerable<string> variables)
    {
        foreach (var variable in variables)
        {
            arguments.AddRange(["--env", variable]);
        }
    }
}
