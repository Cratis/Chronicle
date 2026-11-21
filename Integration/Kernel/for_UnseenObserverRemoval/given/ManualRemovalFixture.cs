// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Kernel.Integration.for_UnseenObserverRemoval.given;

/// <summary>
/// The kernel with the removal on and a sweep that never starts by itself, so a specification decides when it runs.
/// </summary>
public class ManualRemovalFixture : UnseenObserverFixture
{
    /// <inheritdoc/>
    public override IReadOnlyDictionary<string, string?> Settings => new Dictionary<string, string?>(CommonSettings)
    {
        ["Observers:UnseenRemovalEnabled"] = "true",
        ["Observers:UnseenRemovalSweepInterval"] = "1.00:00:00",
        ["Observers:QuarantineOnFailedPartitionCount"] = "3"
    };
}
