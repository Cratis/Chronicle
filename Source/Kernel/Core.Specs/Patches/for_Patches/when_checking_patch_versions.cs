// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Choreography.Patching;
using Cratis.Chronicle.Patches;
using Cratis.Chronicle.Sys;

namespace Cratis.Chronicle.Core.Specs.Patches.for_Patches;

/// <summary>
/// A patch is applied only when its version is above the recorded system version, so one versioned for a major release
/// that does not exist can never run. Pull request builds carry the previous release's version, so a patch written for the
/// next release of the same major is legitimate and only a later major is rejected.
/// </summary>
public class when_checking_patch_versions : Specification
{
    IEnumerable<ICanApplyPatch> _patchesForALaterMajor;
    Cratis.Chronicle.Concepts.System.SemanticVersion _kernelVersion;

    void Because()
    {
        var assembly = typeof(RepeatMoveSystemEventStoreDatabases).Assembly;
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? string.Empty;
        _kernelVersion = KernelVersion.Parse(informational);

        // A local build without a release version has nothing to compare against.
        _patchesForALaterMajor = !KernelVersion.IsKnown(_kernelVersion)
            ? []
            : assembly.GetTypes()
                .Where(_ => typeof(ICanApplyPatch).IsAssignableFrom(_) && _.IsClass && !_.IsAbstract && _.Namespace?.StartsWith("Cratis.Chronicle.Patches", StringComparison.Ordinal) == true)
                .Select(_ => (ICanApplyPatch)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(_))
                .Where(_ => _.Version.Major > _kernelVersion.Major)
                .ToArray();
    }

    [Fact] void should_not_have_a_patch_for_a_later_major_than_the_kernel() => _patchesForALaterMajor.ShouldBeEmpty();
}
