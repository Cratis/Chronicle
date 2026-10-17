// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_OldKernelOccurrences;

/// <summary>
/// A fact that is reported as skipped unless released-kernel compatibility specs were explicitly requested.
/// </summary>
/// <remarks>
/// These specs need a release-stamped build (-p:Version=19.x) and a released kernel image, neither of
/// which the ordinary integration run provides. Set <see cref="OldKernelFixture.EnableVariable"/> to 1 to run them.
/// </remarks>
public sealed class OldKernelFactAttribute : FactAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OldKernelFactAttribute"/> class.
    /// </summary>
    public OldKernelFactAttribute()
    {
        if (!OldKernelFixture.Enabled)
        {
            Skip = $"Released-kernel compatibility specs are opt-in. Set {OldKernelFixture.EnableVariable}=1 and build with -p:Version=19.74.0; see for_OldKernelOccurrences/README.md.";
        }
    }
}
