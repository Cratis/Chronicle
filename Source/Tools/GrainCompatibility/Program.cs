// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Tools.GrainCompatibility;
using Cratis.Chronicle.Tools.GrainCompatibility.Payloads;

// During a rolling upgrade, silos of the previous release and of this one call each other's grains. That only works
// when this release adds to the grain contracts and changes nothing the previous release relies on. This compares
// the OrleansContracts.txt of this build against the previous release's and says what, if anything, breaks that.
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

if (options.SelfTest)
{
    var grainFailures = SelfTest.Run(Console.Out);
    var payloadFailures = PayloadSelfTest.Run(Console.Out);
    return grainFailures + payloadFailures == 0 ? 0 : 1;
}

var contractKind = "grain";
try
{
    var hasBreaks = false;
    if (options.Baseline is not null)
    {
        var baseline = ContractManifest.Parse(await File.ReadAllTextAsync(options.Baseline));
        var current = ContractManifest.Parse(await File.ReadAllTextAsync(options.Current!));

        // An empty manifest on either side compares nothing and would pass anything, so it is not a result.
        if (!baseline.ActiveInterfaces.Any() || !current.ActiveInterfaces.Any())
        {
            await Console.Error.WriteLineAsync($"No active grain interfaces in {(baseline.ActiveInterfaces.Any() ? options.Current : options.Baseline)}, so nothing could be compared.");
            return 2;
        }

        var breaks = ContractComparison.Compare(baseline, current);
        await Console.Out.WriteAsync(ReportRenderer.ToText(options.BaselineName, baseline, current, breaks));
        if (options.GitHub)
        {
            await Console.Out.WriteAsync(ReportRenderer.ToWorkflowCommands(options.BaselineName, breaks));
        }

        hasBreaks = breaks.Count > 0;
    }

    contractKind = "payload";
    if (options.PayloadBaseline is not null)
    {
        var baseline = PayloadContractManifest.Parse(await File.ReadAllTextAsync(options.PayloadBaseline));
        var current = PayloadContractManifest.Parse(await File.ReadAllTextAsync(options.PayloadCurrent!));
        if (baseline.Types.Count == 0 || current.Types.Count == 0)
        {
            await Console.Error.WriteLineAsync($"No payload types in {(baseline.Types.Count == 0 ? options.PayloadBaseline : options.PayloadCurrent)}, so nothing could be compared.");
            return 2;
        }

        var breaks = PayloadContractComparison.Compare(baseline, current);
        await Console.Out.WriteAsync(ReportRenderer.ToText(options.BaselineName, baseline, current, breaks));
        if (options.GitHub)
        {
            await Console.Out.WriteAsync(ReportRenderer.ToWorkflowCommands(options.BaselineName, breaks));
        }

        hasBreaks |= breaks.Count > 0;
    }

    return hasBreaks ? 1 : 0;
}
catch (Exception ex) when (ex is InvalidContractManifest or InvalidPayloadContractManifest or IOException or UnauthorizedAccessException)
{
    await Console.Error.WriteLineAsync($"Could not read the {contractKind} contracts: {ex.Message}");
    return 2;
}
