// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>The command line of one storage benchmark run.</summary>
public sealed class StorageRunOptions
{
    /// <summary>The flag that confirms a long run.</summary>
    public const string ConfirmFlag = "--yes-i-know-this-is-long";

    static readonly Dictionary<string, Action<StorageRunOptions, string>> _handlers = new(StringComparer.Ordinal)
    {
        ["--profile"] = (options, value) => options.Profile = BenchmarkProfile.Parse(value),
        ["--providers"] = (options, value) => options.Providers = StorageProviderNames.Parse(value),
        ["--output"] = (options, value) => options.OutputDirectory = value,
        ["--max-load"] = (options, value) => options.MaxLoad = ParseDouble("--max-load", value),
        ["--payload-bytes"] = (options, value) => options._payloadBytes = ParseInt("--payload-bytes", value),
        ["--mongo-topology"] = (options, value) => options.MongoTopology = value,
        ["--filter"] = (options, value) => options.Filter = value,
        ["--repetition"] = (options, value) => options.Repetition = ParseInt("--repetition", value),
    };

    int? _payloadBytes;

    /// <summary>Gets the current options. Benchmarks run in process, so they read the options from here.</summary>
    public static StorageRunOptions Current { get; set; } = new();

    /// <summary>Gets the profile.</summary>
    public BenchmarkProfile Profile { get; private set; } = BenchmarkProfile.Smoke;

    /// <summary>Gets the providers to run, in order.</summary>
    public IReadOnlyList<StorageProvider> Providers { get; private set; } = [StorageProvider.Sqlite];

    /// <summary>Gets the directory results are written to.</summary>
    public string OutputDirectory { get; private set; } = Path.Combine(Directory.GetCurrentDirectory(), "results");

    /// <summary>Gets the explicit load limit, or <see langword="null"/> for the default.</summary>
    public double? MaxLoad { get; private set; }

    /// <summary>Gets whether a long run was confirmed.</summary>
    public bool Confirmed { get; private set; }

    /// <summary>Gets the MongoDB topology: `single` (a one-node replica set).</summary>
    public string MongoTopology { get; private set; } = "single";

    /// <summary>Gets the BenchmarkDotNet filter, `*` for everything.</summary>
    public string Filter { get; private set; } = "*";

    /// <summary>Gets the repetition number, recorded in file names so interleaved repetitions do not overwrite each other.</summary>
    public int Repetition { get; private set; } = 1;

    /// <summary>Parses the command line.</summary>
    /// <param name="args">The arguments.</param>
    /// <returns>The options.</returns>
    /// <exception cref="ArgumentException">An argument is unknown, lacks its value or has an invalid value.</exception>
    public static StorageRunOptions Parse(IReadOnlyList<string> args)
    {
        var options = new StorageRunOptions();
        for (var index = 0; index < args.Count; index++)
        {
            var name = args[index];
            if (name == ConfirmFlag)
            {
                options.Confirmed = true;
                continue;
            }

            if (!_handlers.TryGetValue(name, out var handler)) throw new ArgumentException($"Unknown argument '{name}'.");
            if (index + 1 >= args.Count) throw new ArgumentException($"{name} needs a value.");
            handler(options, args[++index]);
        }

        return options.Validated();
    }

    /// <summary>Creates a copy of these options that runs one provider only.</summary>
    /// <param name="provider">The provider.</param>
    /// <returns>The copy.</returns>
    public StorageRunOptions ForProvider(StorageProvider provider)
    {
        var copy = (StorageRunOptions)MemberwiseClone();
        copy.Providers = [provider];
        return copy;
    }

    static int ParseInt(string name, string value) =>
        int.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : throw new ArgumentException($"{name} needs an integer, got '{value}'.");

    static double ParseDouble(string name, string value) =>
        double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : throw new ArgumentException($"{name} needs a number, got '{value}'.");

    StorageRunOptions Validated()
    {
        if (Providers.Count == 0) throw new ArgumentException("At least one provider is needed.");
        if (MongoTopology != "single") throw new ArgumentException("Only --mongo-topology single is implemented; rs3 is a follow-up.");
        if (Repetition < 1) throw new ArgumentException("--repetition must be at least 1.");
        if (_payloadBytes is not null)
        {
            if (_payloadBytes.Value < 1) throw new ArgumentException("--payload-bytes must be at least 1.");
            Profile = Profile.WithPayloadBytes(_payloadBytes.Value);
        }

        return this;
    }
}
