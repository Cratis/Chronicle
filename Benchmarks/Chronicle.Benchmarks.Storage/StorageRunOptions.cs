// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>The command line of one storage benchmark run.</summary>
public sealed class StorageRunOptions
{
    /// <summary>The flag that confirms a long run.</summary>
    public const string ConfirmFlag = "--yes-i-know-this-is-long";

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
    /// <exception cref="ArgumentException">An argument is unknown or lacks its value.</exception>
    public static StorageRunOptions Parse(IReadOnlyList<string> args)
    {
        var options = new StorageRunOptions();
        var payload = (int?)null;
        for (var index = 0; index < args.Count; index++)
        {
            string Value() => index + 1 < args.Count ? args[++index] : throw new ArgumentException($"{args[index]} needs a value.");
            switch (args[index])
            {
                case "--profile": options.Profile = BenchmarkProfile.Parse(Value()); break;
                case "--providers": options.Providers = StorageProviderNames.Parse(Value()); break;
                case "--output": options.OutputDirectory = Value(); break;
                case "--max-load": options.MaxLoad = double.Parse(Value(), System.Globalization.CultureInfo.InvariantCulture); break;
                case "--payload-bytes": payload = int.Parse(Value(), System.Globalization.CultureInfo.InvariantCulture); break;
                case "--mongo-topology": options.MongoTopology = Value(); break;
                case "--filter": options.Filter = Value(); break;
                case "--repetition": options.Repetition = int.Parse(Value(), System.Globalization.CultureInfo.InvariantCulture); break;
                case ConfirmFlag: options.Confirmed = true; break;
                default: throw new ArgumentException($"Unknown argument '{args[index]}'.");
            }
        }

        if (options.Providers.Count == 0) throw new ArgumentException("At least one provider is needed.");
        if (options.MongoTopology != "single") throw new ArgumentException("Only --mongo-topology single is implemented; rs3 is a follow-up.");
        if (payload is not null) options.Profile = options.Profile.WithPayloadBytes(payload.Value);
        return options;
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
}
