// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrainCompatibility;

/// <summary>
/// What the tool was asked to do.
/// </summary>
/// <param name="Baseline">The OrleansContracts.txt of the previous release.</param>
/// <param name="Current">The OrleansContracts.txt being checked.</param>
/// <param name="BaselineName">What to call the baseline in the report, typically its release version.</param>
/// <param name="GitHub">Whether to emit GitHub workflow commands for each break.</param>
/// <param name="SelfTest">Whether to check the comparison itself against planted breaks instead.</param>
public record Options(string? Baseline, string? Current, string BaselineName, bool GitHub, bool SelfTest)
{
    /// <summary>
    /// How to invoke the tool.
    /// </summary>
    public static string Usage { get; } = string.Join(
        Environment.NewLine,
        "Usage: GrainCompatibility --baseline <OrleansContracts.txt> --current <OrleansContracts.txt> [options]",
        "       GrainCompatibility --self-test",
        string.Empty,
        "  --baseline <path>        The grain contracts of the previous release.",
        "  --current <path>         The grain contracts being checked.",
        "  --baseline-name <name>   What to call the baseline in the report. Defaults to 'baseline'.",
        "  --github                 Emit GitHub workflow commands for each break.",
        "  --self-test              Check that every kind of break is still detected, then exit.",
        string.Empty,
        "Exits 0 when the current contracts serve the baseline, 1 when they do not, 2 when it could not tell.");

    /// <summary>
    /// Parses command line arguments.
    /// </summary>
    /// <param name="args">The arguments.</param>
    /// <returns>The parsed <see cref="Options"/>.</returns>
    /// <exception cref="InvalidArguments">Thrown when the arguments do not describe something the tool can do.</exception>
    public static Options Parse(string[] args)
    {
        string? baseline = null;
        string? current = null;
        var baselineName = "baseline";
        var github = false;
        var selfTest = false;

        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--baseline":
                    baseline = ValueOf(args, ref index);
                    break;
                case "--current":
                    current = ValueOf(args, ref index);
                    break;
                case "--baseline-name":
                    baselineName = ValueOf(args, ref index);
                    break;
                case "--github":
                    github = true;
                    break;
                case "--self-test":
                    selfTest = true;
                    break;
                default:
                    throw new InvalidArguments($"Unknown argument '{args[index]}'.");
            }
        }

        if (selfTest)
        {
            return new(null, null, baselineName, github, true);
        }

        if (baseline is null || current is null)
        {
            throw new InvalidArguments("Both --baseline and --current are required.");
        }

        return new(baseline, current, baselineName, github, false);
    }

    static string ValueOf(string[] args, ref int index)
    {
        if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
        {
            throw new InvalidArguments($"'{args[index]}' needs a value.");
        }

        return args[++index];
    }
}
