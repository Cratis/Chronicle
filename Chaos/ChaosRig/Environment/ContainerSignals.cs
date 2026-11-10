// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Chaos;

internal static class ContainerSignals
{
    public static Task<string> Send(string container, string signal, CancellationToken cancellation) => NativeCommand.Run("docker", ["kill", "--signal", signal, container], cancellation);
    public static Task<string> Pause(string container, CancellationToken cancellation) => NativeCommand.Run("docker", ["pause", container], cancellation);
    public static Task<string> Unpause(string container, CancellationToken cancellation) => NativeCommand.Run("docker", ["unpause", container], cancellation);
    public static Task<string> Restart(string container, CancellationToken cancellation) => NativeCommand.Run("docker", ["start", container], cancellation);
}
