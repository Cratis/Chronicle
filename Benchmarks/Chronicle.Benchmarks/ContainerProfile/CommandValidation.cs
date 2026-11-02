// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.ContainerProfile;

/// <summary>Rejects unsafe or incomplete command builders before execution.</summary>
internal static class CommandValidation
{
    static readonly string[] _memoryOptions = ["--memory", "--memory-swap"];

    /// <summary>Checks resource limits, ownership labels, deadlines and forbidden operations.</summary>
    /// <param name="command">The built command.</param>
    /// <param name="cell">The expected cell limits.</param>
    /// <param name="session">The expected session label.</param>
    /// <returns>All discovered violations.</returns>
    public static IEnumerable<string> Validate(DockerCommand command, MatrixCell cell, string session)
    {
        if (command.Deadline <= TimeSpan.Zero)
        {
            yield return $"{command.Purpose}: missing positive deadline";
        }
        if (command.Arguments.Any(argument => argument.Contains("prune", StringComparison.OrdinalIgnoreCase)))
        {
            yield return $"{command.Purpose}: forbidden prune argument";
        }
        if (command.Arguments[0] == "create" || command.Arguments[0] == "run" || command.Arguments.Take(2).SequenceEqual(["network", "create"]))
        {
            foreach (var label in new[] { "cratis.container-profile=1", $"cratis.container-profile.session={session}" }.Where(label => !Has(command, "--label", label)))
            {
                yield return $"{command.Purpose}: missing label {label}";
            }
        }
        if (command.Purpose == "create kernel")
        {
            foreach (var option in _memoryOptions.Where(option => !Has(command, option, cell.Memory)))
            {
                yield return $"{cell.Id}: missing or incorrect {option}";
            }
            if (!Has(command, "--cpus", cell.Cpus.ToString(System.Globalization.CultureInfo.InvariantCulture)))
            {
                yield return $"{cell.Id}: missing or incorrect --cpus";
            }
        }
    }

    static bool Has(DockerCommand command, string option, string value) =>
        command.Arguments.Zip(command.Arguments.Skip(1)).Any(pair => pair.First == option && pair.Second == value);
}
