// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Kernel.Integration.for_UnseenObserverRemoval.given;

/// <summary>
/// The kernel with the removal at its default, which is off, although the period is short and the sweep runs every second.
/// </summary>
public class DisabledRemovalFixture : UnseenObserverFixture
{
    /// <inheritdoc/>
    public override IReadOnlyDictionary<string, string?> Settings => new Dictionary<string, string?>(CommonSettings)
    {
        ["Observers:UnseenRemovalSweepInterval"] = "00:00:01"
    };
}
