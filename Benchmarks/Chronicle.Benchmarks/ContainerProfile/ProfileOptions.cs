// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Chronicle.Benchmarks.ContainerProfile;

/// <summary>The profiling CLI options and workload defaults.</summary>
internal sealed record ProfileOptions
{
    static readonly string[] _commands = ["plan", "run", "build-images", "r2r-check", "cleanup"];

    static readonly Dictionary<string, Func<ProfileOptions, ProfileOptions>> _flags = new(StringComparer.Ordinal)
    {
        ["--gc-trace"] = options => options with { GcTrace = true },
        ["--verbose"] = options => options with { Verbose = true },
        ["--allow-noisy-host"] = options => options with { AllowNoisyHost = true }
    };

    static readonly Dictionary<string, Func<ProfileOptions, ProfileArgument, ProfileOptions?>> _values = new(StringComparer.Ordinal)
    {
        ["--matrix"] = WithMatrix,
        ["--cells"] = (options, argument) => options with { Cells = argument.Value.Split(',', StringSplitOptions.RemoveEmptyEntries) },
        ["--results"] = (options, argument) => options with { Results = Path.GetFullPath(argument.Value) },
        ["--image"] = (options, argument) => options with { Image = argument.Value },
        ["--session"] = (options, argument) => options with { Session = argument.Value },
        ["--variants"] = WithVariants,
        ["--max-host-load"] = WithLoad
    };

    /// <summary>Gets the subcommand.</summary>
    public string Command { get; init; } = "plan";

    /// <summary>Gets the matrix name.</summary>
    public string Matrix { get; init; } = "core";

    /// <summary>Gets optional substring filters.</summary>
    public string[] Cells { get; init; } = [];

    /// <summary>Gets untraced repetitions per cell.</summary>
    public int Repetitions { get; init; } = 3;

    /// <summary>Gets whether to add a separate trace repetition.</summary>
    public bool GcTrace { get; init; }

    /// <summary>Gets whether to render every plan command.</summary>
    public bool Verbose { get; init; }

    /// <summary>Gets whether noisy-host runs are explicitly allowed.</summary>
    public bool AllowNoisyHost { get; init; }

    /// <summary>Gets the one-minute load-average ceiling.</summary>
    public double MaxHostLoad { get; init; } = Environment.ProcessorCount * 0.5;

    /// <summary>Gets the session output directory.</summary>
    public string Results { get; init; } = Path.Combine("Benchmarks", "results", "container-profile", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture));

    /// <summary>Gets the publish variants to build.</summary>
    public string[] Variants { get; init; } = ["r2r", "jit"];

    /// <summary>Gets the image for offline PE inspection.</summary>
    public string Image { get; init; } = string.Empty;

    /// <summary>Gets the required cleanup session identity.</summary>
    public string Session { get; init; } = string.Empty;

    /// <summary>Gets the workload scale.</summary>
    public WorkloadSettings Workload { get; init; } = new();

    /// <summary>Parses arguments without starting processes or writing files.</summary>
    /// <param name="arguments">The subcommand and arguments.</param>
    /// <returns>The parsed options or a safe diagnostic.</returns>
    public static (ProfileOptions? Options, string? Error) Parse(string[] arguments)
    {
        if (!HasCommand(arguments))
        {
            return (null, "Expected plan, build-images, run, r2r-check or cleanup.");
        }
        var options = new ProfileOptions { Command = arguments[0] };
        for (var i = 1; i < arguments.Length; i++)
        {
            var key = arguments[i];
            var flag = ApplyFlag(options, key);
            if (flag is not null)
            {
                options = flag;
                continue;
            }
            if (++i >= arguments.Length)
            {
                return (null, $"Missing value for {key}.");
            }
            var updated = ApplyValue(options, new ProfileArgument(key, arguments[i]));
            if (updated is null)
            {
                return (null, $"Invalid option or value: {key}.");
            }
            options = updated;
        }
        return Validate(options);
    }

    static bool HasCommand(string[] arguments) => arguments.Length > 0 && _commands.Contains(arguments[0]);

    static (ProfileOptions? Options, string? Error) Validate(ProfileOptions options)
    {
        if (options.Command == "r2r-check" && options.Image.Length == 0)
        {
            return (null, "r2r-check requires --image <ref>.");
        }
        if (options.Command == "cleanup" && string.IsNullOrWhiteSpace(options.Session))
        {
            return (null, "cleanup requires --session <id>.");
        }
        return (options, null);
    }

    static ProfileOptions? ApplyFlag(ProfileOptions options, string key) =>
        _flags.TryGetValue(key, out var apply) ? apply(options) : null;

    static ProfileOptions? ApplyValue(ProfileOptions options, ProfileArgument argument) =>
        _values.TryGetValue(argument.Key, out var apply) ? apply(options, argument) : ApplyNumber(options, argument);

    static ProfileOptions? WithMatrix(ProfileOptions options, ProfileArgument argument) =>
        argument.Value == "core" || argument.Value == "full" ? options with { Matrix = argument.Value } : null;

    static ProfileOptions? WithVariants(ProfileOptions options, ProfileArgument argument)
    {
        var variants = argument.Value.Split(',');
        return variants.All(variant => variant == "r2r" || variant == "jit")
            ? options with { Variants = variants.Distinct().ToArray() }
            : null;
    }

    static ProfileOptions? WithLoad(ProfileOptions options, ProfileArgument argument)
    {
        if (!double.TryParse(argument.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var load))
        {
            return null;
        }
        if (!double.IsFinite(load))
        {
            return null;
        }
        return load > 0 ? options with { MaxHostLoad = load } : null;
    }

    static ProfileOptions? ApplyNumber(ProfileOptions options, ProfileArgument argument)
    {
        if (!int.TryParse(argument.Value, CultureInfo.InvariantCulture, out var number))
        {
            return null;
        }
        if (number <= 0)
        {
            return null;
        }
        if (argument.Key == "--repetitions")
        {
            return options with { Repetitions = number };
        }
        var workload = options.Workload.WithOption(argument.Key, number);
        return workload is null ? null : options with { Workload = workload };
    }
}
