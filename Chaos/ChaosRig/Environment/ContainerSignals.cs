// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Chaos;

/// <summary>
/// Controls dedicated fault-target containers through Docker.
/// </summary>
internal static class ContainerSignals
{
    static readonly ContainerControl _control = new((arguments, cancellation) => NativeCommand.Run("docker", arguments, cancellation), Console.WriteLine);

    /// <summary>
    /// Sends a signal to a container.
    /// </summary>
    /// <param name="container">The container identifier.</param>
    /// <param name="signal">The signal name.</param>
    /// <param name="cancellation">The command cancellation token.</param>
    /// <returns>The command output.</returns>
    public static Task<string> Send(string container, string signal, CancellationToken cancellation) => signal == "KILL"
        ? _control.Apply(container, "kill", cancellation)
        : NativeCommand.Run("docker", ["kill", "--signal", signal, container], cancellation);

    /// <summary>
    /// Freezes all container processes.
    /// </summary>
    /// <param name="container">The container identifier.</param>
    /// <param name="cancellation">The command cancellation token.</param>
    /// <returns>The command output.</returns>
    public static Task<string> Pause(string container, CancellationToken cancellation) => _control.Apply(container, "pause", cancellation);

    /// <summary>
    /// Resumes all container processes.
    /// </summary>
    /// <param name="container">The container identifier.</param>
    /// <param name="cancellation">The command cancellation token.</param>
    /// <returns>The command output.</returns>
    public static Task<string> Unpause(string container, CancellationToken cancellation) => _control.Apply(container, "unpause", cancellation);

    /// <summary>
    /// Starts a stopped container.
    /// </summary>
    /// <param name="container">The container identifier.</param>
    /// <param name="cancellation">The command cancellation token.</param>
    /// <returns>The command output.</returns>
    public static Task<string> Restart(string container, CancellationToken cancellation) => _control.Apply(container, "start", cancellation);
}
