// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Cratis.Chronicle.Chaos;

/// <summary>
/// Delivers signals directly to an owned Unix child without spawning a command.
/// </summary>
internal static partial class NativeSignals
{
    /// <summary>
    /// Sends STOP or CONT to an owned child process.
    /// </summary>
    /// <param name="processId">The positive child process identifier.</param>
    /// <param name="signal">The STOP or CONT signal name.</param>
    /// <exception cref="ArgumentOutOfRangeException">The identifier or signal is invalid.</exception>
    /// <exception cref="PlatformNotSupportedException">The host is not macOS or Linux.</exception>
    /// <exception cref="Win32Exception">The operating system refused the signal.</exception>
    internal static void Send(int processId, string signal)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);
        if (!OperatingSystem.IsMacOS() && !OperatingSystem.IsLinux()) throw new PlatformNotSupportedException("Child signals require macOS or Linux.");
        var number = NumberFor(signal, OperatingSystem.IsMacOS());
        if (Kill(processId, number) == 0) return;
        var error = Marshal.GetLastPInvokeError();

        // A child that exited while paused needs no CONT; never signal a process group.
        if (error == 3 && signal == "CONT") return;
        throw new Win32Exception(error, $"Could not send {signal} to owned child {processId}.");
    }

    /// <summary>
    /// Maps supported signal names to their platform ABI values.
    /// </summary>
    /// <param name="signal">The STOP or CONT signal name.</param>
    /// <param name="macOS">Whether to use the Darwin ABI instead of Linux.</param>
    /// <returns>The native signal number.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The signal is unsupported.</exception>
    internal static int NumberFor(string signal, bool macOS) => signal switch
    {
        "STOP" => macOS ? 17 : 19,
        "CONT" => macOS ? 19 : 18,
        _ => throw new ArgumentOutOfRangeException(nameof(signal), signal, "Only STOP and CONT are supported.")
    };

    [LibraryImport("libc", EntryPoint = "kill", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static partial int Kill(int processId, int signal);
}
