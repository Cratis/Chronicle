// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Chronicle.Tools.WireCompatibility;

/// <summary>
/// What the tool was asked to compare.
/// </summary>
/// <param name="Major">The major version whose every released minor is a baseline.</param>
/// <param name="Since">The oldest release still treated as a baseline, when a floor has been declared.</param>
/// <param name="BaselineVersion">An explicit baseline version, overriding <paramref name="Major"/>.</param>
/// <param name="BaselineAssembly">A local contracts assembly to use as the baseline instead of downloading one.</param>
/// <param name="Current">The current descriptor set, or the contracts assembly to generate one from.</param>
/// <param name="ImportPath">The directory holding the proto files imports resolve against.</param>
/// <param name="GitHub">Whether to emit GitHub workflow commands alongside the report.</param>
/// <param name="AllowMissingBaseline">Whether a major with nothing released yet passes instead of failing.</param>
public record Options(
    int? Major,
    string? Since,
    string? BaselineVersion,
    string? BaselineAssembly,
    string Current,
    string ImportPath,
    bool GitHub,
    bool AllowMissingBaseline)
{
    /// <summary>
    /// How to invoke the tool.
    /// </summary>
    public static string Usage { get; } = string.Join(
        Environment.NewLine,
        "Usage: WireCompatibility --current <chronicle.desc|Contracts.dll> [options]",
        string.Empty,
        "  --major <n>              Compare against every released minor of major version n.",
        "  --since <version>        The oldest release to compare against. Must be in the same major as --major.",
        "  --baseline <version>     Compare against an explicit released version.",
        "  --baseline-assembly <p>  Compare against a local contracts assembly.",
        "  --current <path>         The descriptor set, or contracts assembly, to check. Required.",
        "  --import-path <dir>      Directory to resolve proto imports from. Defaults to the current file's directory.",
        "  --github                 Emit GitHub workflow commands for each breaking change.",
        "  --allow-missing-baseline Pass when the major has no release yet, rather than failing.",
        string.Empty,
        "Exactly one of --major, --baseline or --baseline-assembly is required.",
        "Exits 0 when nothing breaks, 1 when something does, 2 when it could not tell.");

    /// <summary>
    /// Parses command line arguments.
    /// </summary>
    /// <param name="args">The arguments.</param>
    /// <returns>The parsed options.</returns>
    /// <exception cref="InvalidArguments">Thrown when the arguments do not describe a comparison that can be made.</exception>
    public static Options Parse(string[] args)
    {
        int? major = null;
        string? since = null;
        string? baselineVersion = null;
        string? baselineAssembly = null;
        string? current = null;
        string? importPath = null;
        var github = false;
        var allowMissingBaseline = false;

        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--major":
                    major = int.Parse(Next(args, ref index, "--major"), CultureInfo.InvariantCulture);
                    break;
                case "--since":
                    since = Next(args, ref index, "--since");
                    break;
                case "--baseline":
                    baselineVersion = Next(args, ref index, "--baseline");
                    break;
                case "--baseline-assembly":
                    baselineAssembly = Next(args, ref index, "--baseline-assembly");
                    break;
                case "--current":
                    current = Next(args, ref index, "--current");
                    break;
                case "--import-path":
                    importPath = Next(args, ref index, "--import-path");
                    break;
                case "--github":
                    github = true;
                    break;
                case "--allow-missing-baseline":
                    allowMissingBaseline = true;
                    break;
                default:
                    throw new InvalidArguments($"'{args[index]}' is not an option this tool takes.");
            }
        }

        if (string.IsNullOrEmpty(current))
        {
            throw new InvalidArguments("--current is required.");
        }

        var baselines = new object?[] { major, baselineVersion, baselineAssembly }.Count(_ => _ is not null);
        if (baselines != 1)
        {
            throw new InvalidArguments("Exactly one of --major, --baseline or --baseline-assembly is required.");
        }

        if (since is not null && major is null)
        {
            throw new InvalidArguments("--since only means something alongside --major, which is what decides the set of baselines it narrows.");
        }

        // A floor from another major narrows nothing when it is older and everything when it is newer, and either
        // way it reads like a working floor. Saying so here is the difference between a misconfigured gate and a
        // silent one.
        if (since is not null && !string.Equals(since.Split('.')[0], major?.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal))
        {
            throw new InvalidArguments($"--since {since} is not in major {major}, so it says nothing about which of that major's releases are baselines.");
        }

        return new(
            major,
            since,
            baselineVersion,
            baselineAssembly,
            current,
            importPath ?? Path.GetDirectoryName(Path.GetFullPath(current)) ?? ".",
            github,
            allowMissingBaseline);
    }

    static string Next(string[] args, ref int index, string option) =>
        ++index < args.Length ? args[index] : throw new InvalidArguments($"{option} needs a value.");
}
