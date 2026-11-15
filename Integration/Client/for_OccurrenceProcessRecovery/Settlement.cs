// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Chronicle.Integration.for_OccurrenceProcessRecovery;

public record Settlement
{
    public required string OccurrenceId { get; init; }
    public required long Epoch { get; init; }

    /// <summary>
    /// Gets the effect receipt status name reported by the child process. A string because the status
    /// enum exists in both the client and the kernel assembly referenced by this project.
    /// </summary>
    public required string Status { get; init; }
    public required long WinnerEpoch { get; init; }
    public required int ProcessId { get; init; }

    public static Settlement Parse(string line)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        parts.Length.ShouldEqual(6);
        parts[0].ShouldEqual("SETTLED");
        return new()
        {
            OccurrenceId = parts[1],
            Epoch = long.Parse(parts[2], CultureInfo.InvariantCulture),
            Status = parts[3],
            WinnerEpoch = long.Parse(parts[4], CultureInfo.InvariantCulture),
            ProcessId = int.Parse(parts[5], CultureInfo.InvariantCulture)
        };
    }
}
