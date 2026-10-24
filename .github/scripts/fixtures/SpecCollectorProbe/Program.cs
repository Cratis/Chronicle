// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// CI process fixture for the pinned coverage collector: `exit <code>` returns a known code;
// `hang <pidfile>` starts one owned child, records both PIDs, then waits (bounded, never forever).
using System.Diagnostics;

var failsafe = TimeSpan.FromSeconds(120);
switch (args)
{
    case ["exit", var code] when int.TryParse(code, out var exitCode):
        return exitCode;
    case ["hang", var pidFile]:
        using (var child = Process.Start(Environment.ProcessPath!, [Environment.GetCommandLineArgs()[0], "sleep"]))
        {
            File.WriteAllText(pidFile, $"{Environment.ProcessId}\n{child!.Id}\n");
            Thread.Sleep(failsafe);
        }

        return 0;
    case ["sleep"]:
        Thread.Sleep(failsafe);
        return 0;
    default:
        return 2;
}
