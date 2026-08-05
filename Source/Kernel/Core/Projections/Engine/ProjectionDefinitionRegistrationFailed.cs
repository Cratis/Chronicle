// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// The exception that is thrown when registering a single projection definition fails.
/// </summary>
/// <remarks>
/// Definitions are registered as a batch and concurrently, so a failure surfaces from the batch without saying which
/// definition produced it. This carries that attribution outwards, letting the failure name the one definition that
/// failed rather than every definition that happened to be registered alongside it.
/// </remarks>
/// <param name="identifier">The <see cref="ProjectionId"/> of the definition that failed to register.</param>
/// <param name="innerException">The underlying cause.</param>
[GenerateSerializer]
public class ProjectionDefinitionRegistrationFailed(ProjectionId identifier, Exception innerException)
    : Exception($"Failed to register projection '{identifier}'. Root cause: {innerException.GetBaseException().Message}", innerException)
{
    /// <summary>
    /// Gets the <see cref="ProjectionId"/> of the definition that failed to register.
    /// </summary>
    [Id(0)]
    public ProjectionId Identifier { get; } = identifier;

    /// <summary>
    /// Try to find the <see cref="ProjectionId"/> a failure is attributed to.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to look through.</param>
    /// <param name="identifier">The <see cref="ProjectionId"/> that was found, if any.</param>
    /// <returns>True if an attributed identifier was found, false if not.</returns>
    /// <remarks>
    /// The attribution can be nested arbitrarily deep by the time it reaches a caller - grain calls and aggregated
    /// task failures both wrap what they carry - so the whole exception tree is searched rather than only the
    /// outermost exception.
    /// </remarks>
    public static bool TryFindIdentifier(Exception? exception, [NotNullWhen(true)] out ProjectionId? identifier)
    {
        switch (exception)
        {
            case null:
                identifier = null;
                return false;

            case ProjectionDefinitionRegistrationFailed failed:
                identifier = failed.Identifier;
                return true;

            case AggregateException aggregate:
                foreach (var inner in aggregate.InnerExceptions)
                {
                    if (TryFindIdentifier(inner, out identifier))
                    {
                        return true;
                    }
                }

                identifier = null;
                return false;

            default:
                return TryFindIdentifier(exception.InnerException, out identifier);
        }
    }
}
