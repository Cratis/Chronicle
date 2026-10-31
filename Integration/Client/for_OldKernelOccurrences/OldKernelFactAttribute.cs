// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

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
    /// <param name="sourceFilePath">The source file the fact is declared in, supplied by the compiler.</param>
    /// <param name="sourceLineNumber">The line the fact is declared on, supplied by the compiler.</param>
    public OldKernelFactAttribute(
        [CallerFilePath] string? sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = -1)
        : base(sourceFilePath, sourceLineNumber)
    {
        if (!OldKernelFixture.Enabled)
        {
            Skip = $"Released-kernel compatibility specs are opt-in. Set {OldKernelFixture.EnableVariable}=1 and build with -p:Version=19.74.0; see for_OldKernelOccurrences/README.md.";
        }
    }
}
