// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compatibility;
using Cratis.Chronicle.Tools.WireCompatibility;
using Google.Protobuf.Reflection;

// Everything released within a major has to keep serving every release before it. This compares the wire contract
// at HEAD against each released minor of the major, so a break is caught before it ships rather than by whoever
// upgrades their server without upgrading every client - and so the report says which releases it breaks.
Options options;

try
{
    options = Options.Parse(args);
}
catch (InvalidArguments ex)
{
    await Console.Error.WriteLineAsync(ex.Message);
    await Console.Error.WriteLineAsync();
    await Console.Error.WriteLineAsync(Options.Usage);
    return 2;
}

try
{
#pragma warning disable CA2000 // Ownership passes to NuGetContractsPackage, which is disposed below.
    using var nuget = new NuGetContractsPackage(new HttpClient());
#pragma warning restore CA2000

    var current = WireContractReader.Read(DescriptorSetFor(options.Current, options.ImportPath));
    var all = await Baselines(nuget, options);
    var baselines = options.Since is { } floor ? all.Where(_ => ReleaseVersion.IsAtOrAfter(_, floor)).ToArray() : all;
    var excluded = all.Count - baselines.Count;
    var results = new List<BaselineResult>();

    await Console.Out.WriteLineAsync($"Current:  {options.Current}");

    if (options.Since is not null)
    {
        await Console.Out.WriteLineAsync($"Floor:    {options.Since} ({excluded} earlier release(s) not compared against)");
    }

    await Console.Out.WriteLineAsync();

    // A floor that excludes everything is not the same as a clean run, and a gate that reports them the same way
    // is a gate nobody can tell is switched off. Say it out loud, in the log and in the pull request.
    if (baselines.Count == 0 && options.Since is not null)
    {
        await Console.Out.WriteLineAsync($"No release at or after the declared floor {options.Since}, so nothing was compared.");
        if (options.GitHub)
        {
            await Console.Out.WriteLineAsync($"::warning title=Wire compatibility not compared::The declared floor {options.Since} excludes every released baseline, so this run checked nothing.");
        }

        return 0;
    }

    foreach (var version in baselines)
    {
        var assembly = options.BaselineAssembly ?? await nuget.DownloadAssembly(version);
        var baseline = WireContractReader.Read(DescriptorSets.GenerateFor(assembly, options.ImportPath));
        var report = WireCompatibilityChecker.Check(baseline, current);

        results.Add(new(version, report));
        await Console.Out.WriteLineAsync($"  checked {version}");
    }

    await Console.Out.WriteLineAsync();

    var run = new BaselineRun(results);
    await Console.Out.WriteLineAsync(ReportRenderer.ToText(run));

    if (options.GitHub)
    {
        await Console.Out.WriteAsync(ReportRenderer.ToWorkflowCommands(run));
    }

    return run.IsCompatible ? 0 : 1;
}
catch (NoReleaseForMajor ex) when (options.AllowMissingBaseline)
{
    // The first release of a new major has nothing before it to stay compatible with. That is the one case where
    // there being no baseline is the correct state of the world rather than a broken lookup.
    await Console.Out.WriteLineAsync(ex.Message);
    await Console.Out.WriteLineAsync("Nothing to compare against - this is the first release of the major.");
    return 0;
}
catch (Exception ex)
{
    // Exit 2, never 1: a comparison that could not be made is not the same answer as one that found nothing, and a
    // gate that cannot tell the difference passes on the day the network is down.
    await Console.Error.WriteLineAsync($"Could not compare: {ex.Message}");
    return 2;
}

static async Task<IReadOnlyList<string>> Baselines(NuGetContractsPackage nuget, Options options) =>
    options.Major is { } major
        ? await nuget.StableReleasePerMinor(major)
        : [options.BaselineVersion ?? options.BaselineAssembly!];

static FileDescriptorSet DescriptorSetFor(string path, string importPath) =>
    Path.GetExtension(path).Equals(".dll", StringComparison.OrdinalIgnoreCase)
        ? DescriptorSets.GenerateFor(path, importPath)
        : DescriptorSets.ReadFrom(path);
