// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Chronicle.Benchmarks.ContainerProfile;

/// <summary>The profiling CLI options and workload defaults.</summary>
internal sealed record ProfileOptions
{
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

    /// <summary>Gets the workload scale.</summary>
    public WorkloadSettings Workload { get; init; } = new();

    /// <summary>Parses arguments without starting processes or writing files.</summary>
    /// <param name="arguments">The subcommand and arguments.</param>
    /// <returns>The parsed options or a safe diagnostic.</returns>
    public static (ProfileOptions? Options, string? Error) Parse(string[] arguments)
    {
        if (arguments.Length == 0 || arguments[0] is not ("plan" or "run" or "build-images" or "r2r-check" or "cleanup"))
        {
            return (null, "Expected plan, build-images, run, r2r-check or cleanup.");
        }
        var options = new ProfileOptions { Command = arguments[0] };
        for (var i = 1; i < arguments.Length; i++)
        {
            var key = arguments[i];
            if (key == "--gc-trace" || key == "--verbose" || key == "--allow-noisy-host")
            {
                options = options with { GcTrace = options.GcTrace || key == "--gc-trace", Verbose = options.Verbose || key == "--verbose", AllowNoisyHost = options.AllowNoisyHost || key == "--allow-noisy-host" };
                continue;
            }
            if (++i >= arguments.Length)
            {
                return (null, $"Missing value for {key}.");
            }
            var updated = Apply(options, key, arguments[i]);
            if (updated is null)
            {
                return (null, $"Invalid option or value: {key}.");
            }
            options = updated;
        }
        return options.Command == "r2r-check" && options.Image.Length == 0
            ? (null, "r2r-check requires --image <ref>.")
            : (options, null);
    }

    static ProfileOptions? Apply(ProfileOptions options, string key, string value) => key switch
    {
        "--matrix" when value == "core" || value == "full" => options with { Matrix = value },
        "--cells" => options with { Cells = value.Split(',', StringSplitOptions.RemoveEmptyEntries) },
        "--results" => options with { Results = Path.GetFullPath(value) },
        "--image" => options with { Image = value },
        "--variants" when value.Split(',').All(variant => variant == "r2r" || variant == "jit") => options with { Variants = value.Split(',').Distinct().ToArray() },
        "--max-host-load" when double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var load) && double.IsFinite(load) && load > 0 => options with { MaxHostLoad = load },
        _ => ApplyNumber(options, key, value)
    };

    static ProfileOptions? ApplyNumber(ProfileOptions options, string key, string value)
    {
        if (!int.TryParse(value, CultureInfo.InvariantCulture, out var number) || number <= 0)
        {
            return null;
        }
        return key switch
        {
            "--repetitions" => options with { Repetitions = number },
            "--stores" => options with { Workload = options.Workload with { Stores = number } },
            "--namespaces" => options with { Workload = options.Workload with { Namespaces = number } },
            "--seed-events" => options with { Workload = options.Workload with { SeedEvents = number } },
            "--events" => options with { Workload = options.Workload with { Events = number } },
            "--batch-size" => options with { Workload = options.Workload with { BatchSize = number } },
            "--appenders" => options with { Workload = options.Workload with { Appenders = number } },
            _ => null
        };
    }
}
