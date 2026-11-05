// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.ContainerProfile;

/// <summary>Builds bounded, narrowly projected telemetry commands.</summary>
internal static class MetricCommands
{
    /// <summary>Resolves an ephemeral loopback port.</summary>
    /// <param name="context">The run context.</param>
    /// <param name="port">The container port.</param>
    /// <returns>The bounded command.</returns>
    public static DockerCommand Port(RunContext context, int port) => new("resolve port", ["port", context.Kernel, $"{port}/tcp"], TimeSpan.FromSeconds(30));

    /// <summary>Reads PID 1 status using a labelled, disposable helper.</summary>
    /// <param name="context">The run context.</param>
    /// <returns>The bounded command.</returns>
    public static DockerCommand Status(RunContext context) => new("read PID 1 memory", ["run", "--rm", $"--pid=container:{context.Kernel}", .. context.Labels, "busybox:1.36", "cat", "/proc/1/status"], TimeSpan.FromSeconds(60));

    /// <summary>Reads cgroup usage through Docker statistics.</summary>
    /// <param name="context">The run context.</param>
    /// <returns>The bounded command.</returns>
    public static DockerCommand Stats(RunContext context) => new("read cgroup usage", ["stats", "--no-stream", "--format", "{{json .}}", context.Kernel], TimeSpan.FromSeconds(30));

    /// <summary>Projects only the container state, never its environment.</summary>
    /// <param name="context">The run context.</param>
    /// <returns>The bounded command.</returns>
    public static DockerCommand State(RunContext context) => new("read kernel state", ["inspect", "--format", "{{json .State}}", context.Kernel], TimeSpan.FromSeconds(30));

    /// <summary>Stops the kernel with a bounded grace period.</summary>
    /// <param name="context">The run context.</param>
    /// <returns>The bounded command.</returns>
    public static DockerCommand Stop(RunContext context) => new("stop kernel", ["stop", "--time", "60", context.Kernel], TimeSpan.FromSeconds(75));

    /// <summary>Projects Docker VM CPU, memory, version and platform.</summary>
    /// <returns>The bounded command.</returns>
    public static DockerCommand Info() => new("read Docker VM conditions", ["info", "--format", "{{.NCPU}} {{.MemTotal}} {{.ServerVersion}} {{.OperatingSystem}} {{.Architecture}}"], TimeSpan.FromSeconds(30));

    /// <summary>Resolves the Docker engine architecture.</summary>
    /// <returns>The bounded command.</returns>
    public static DockerCommand Architecture() => new("read Docker architecture", ["version", "--format", "{{.Server.Arch}}"], TimeSpan.FromSeconds(30));

    /// <summary>Projects image identity and uncompressed bytes.</summary>
    /// <param name="image">The image reference.</param>
    /// <returns>The bounded command.</returns>
    public static DockerCommand Image(string image) => new("read image identity and size", ["image", "inspect", "--format", "{{.Id}} {{.Size}}", image], TimeSpan.FromSeconds(30));
}
