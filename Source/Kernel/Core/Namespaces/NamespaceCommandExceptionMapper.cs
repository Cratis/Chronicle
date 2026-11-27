// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Arc.Commands;

namespace Cratis.Chronicle.Namespaces;

/// <summary>Maps typed namespace failures before Arc converts them to unstructured exception messages.</summary>
public sealed class NamespaceCommandExceptionMapper : ICommandExceptionMapper
{
    /// <inheritdoc/>
    public bool TryMap(CommandContext context, Exception exception, [NotNullWhen(true)] out CommandResult? result)
    {
        var code = NamespaceExceptionCodes.For(exception);
        if (code is null)
        {
            result = null;
            return false;
        }

        result = new CommandResult
        {
            CorrelationId = context.CorrelationId,
            ExceptionMessages = [code],
            ExceptionStackTrace = string.Empty
        };
        return true;
    }
}
