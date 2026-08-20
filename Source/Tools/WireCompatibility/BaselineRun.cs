// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.WireCompatibility;

/// <summary>
/// Everything a run of the check found, across every baseline it compared against.
/// </summary>
/// <param name="Results">One result per baseline, oldest first.</param>
public record BaselineRun(IReadOnlyList<BaselineResult> Results)
{
    /// <summary>
    /// Gets a value indicating whether the current contract still serves every baseline.
    /// </summary>
    public bool IsCompatible => Results.All(_ => _.Report.IsCompatible);

    /// <summary>
    /// Gets the baselines the current contract no longer serves, oldest first.
    /// </summary>
    public IEnumerable<BaselineResult> Broken => Results.Where(_ => !_.Report.IsCompatible);

    /// <summary>
    /// Gets every distinct thing that broke, with the baselines each one affects.
    /// </summary>
    /// <remarks>
    /// The same removed method shows up against nearly every baseline, so the findings are folded together and
    /// carry the versions they affect instead. Which baselines a finding covers is the useful part: one that starts
    /// at 16.20.0 says the contract element was added there, and only clients from 16.20 on ever depended on it.
    /// </remarks>
    public IEnumerable<AffectedBaselines> Findings =>
        Results
            .SelectMany(result => result.Report.Incompatibilities.Select(_ => (Baseline: result.Version, Incompatibility: _)))
            .GroupBy(_ => (_.Incompatibility.Kind, _.Incompatibility.Path, _.Incompatibility.Description))
            .Select(_ => new AffectedBaselines(
                _.First().Incompatibility,
                [.. _.Select(baseline => baseline.Baseline)]))
            .OrderBy(_ => _.Incompatibility.Path, StringComparer.Ordinal)
            .ThenBy(_ => _.Incompatibility.Kind);
}
