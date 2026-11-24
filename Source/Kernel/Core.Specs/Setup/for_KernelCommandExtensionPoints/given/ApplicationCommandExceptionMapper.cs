// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Arc.Commands;

namespace Cratis.Chronicle.Setup.for_KernelCommandExtensionPoints.given;

/// <summary>An inert application extension point used to prove kernel-only discovery narrowing.</summary>
public sealed class ApplicationCommandExceptionMapper : ICommandExceptionMapper
{
    /// <inheritdoc/>
    public bool TryMap(CommandContext context, Exception exception, [NotNullWhen(true)] out CommandResult? result)
    {
        result = null;
        return false;
    }
}
