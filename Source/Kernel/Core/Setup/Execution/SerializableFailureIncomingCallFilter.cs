// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Chronicle.Setup.Execution;

/// <summary>
/// Makes a failing grain call answer with an exception the Orleans exception codec can carry.
/// </summary>
/// <remarks>
/// Exceptions outside the codec's supported namespaces are replaced with ordinary wrappers that
/// retain the original type name and message without copying arbitrary exception data. Unsupported
/// inner exceptions are sanitized recursively. Only an exact MongoDB wait-queue-full failure gets a
/// code-owned storage admission category for bounded startup retries. This category is a trusted-silo
/// assertion, not authenticated provenance; supported exceptions still pass through unchanged.
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
        if (exception.GetType() == typeof(MongoWaitQueueFullException))
        {
            var replacement = new InvalidOperationException($"{exception.GetType().FullName}: {exception.Message}");
            replacement.Data[SiblingSiloInstability.StorageAdmissionCategoryKey] = SiblingSiloInstability.MongoWaitQueueFullCategory;
            return replacement;
        }

        var inner = exception.InnerException;
        if (inner is not null && !IsCarried(inner)) inner = Replace(inner);
        return new InvalidOperationException($"{exception.GetType().FullName}: {exception.Message}", inner);
    }
}
