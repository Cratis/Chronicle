// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Setup.Execution;

/// <summary>
/// Makes a failing grain call answer with an exception the Orleans exception codec can carry.
/// </summary>
/// <remarks>
/// The codec only serializes exceptions under its supported namespaces. A driver exception from a
/// storage provider (for example the MongoDB driver's wait-queue-full failure) has no codec, and
/// writing that response throws on the connection's outgoing path, where nothing catches it - the
/// process terminates. Replacing such an exception with a plain one that names the original type
/// and keeps its message turns a crash into an ordinary failed call.
/// </remarks>
public class SerializableFailureIncomingCallFilter : IIncomingGrainCallFilter
{
    static readonly string[] _carriedPrefixes = ["System", "Orleans", "Cratis", "Microsoft"];

    /// <inheritdoc/>
    public async Task Invoke(IIncomingGrainCallContext context)
    {
        try
        {
            await context.Invoke();
        }
        catch (Exception exception) when (!IsCarried(exception))
        {
            throw Replace(exception);
        }
    }

    /// <summary>
    /// Gets whether the exception, and everything inside it, is one the codec carries.
    /// </summary>
    /// <param name="exception">The exception to judge.</param>
    /// <returns>True when it can be serialized as it is.</returns>
    internal static bool IsCarried(Exception exception)
    {
        var ns = exception.GetType().Namespace ?? string.Empty;
        if (!_carriedPrefixes.Any(prefix => ns == prefix || ns.StartsWith(prefix + ".", StringComparison.Ordinal))) return false;
        if (exception is AggregateException aggregate) return aggregate.InnerExceptions.All(IsCarried);
        return exception.InnerException is null || IsCarried(exception.InnerException);
    }

    /// <summary>
    /// Builds a serializable stand-in that keeps the original type name and message.
    /// </summary>
    /// <param name="exception">The exception to replace.</param>
    /// <returns>The replacement.</returns>
    internal static Exception Replace(Exception exception)
    {
        var inner = exception.InnerException;
        if (inner is not null && !IsCarried(inner)) inner = Replace(inner);
        return new InvalidOperationException($"{exception.GetType().FullName}: {exception.Message}", inner);
    }
}
