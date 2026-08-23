// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// The exception that is thrown when one or more projection definitions in a batch fail to register.
/// </summary>
/// <param name="failures">The failures keyed by projection identifier.</param>
[GenerateSerializer]
public class ProjectionDefinitionsRegistrationFailed(
    IReadOnlyDictionary<ProjectionId, ProjectionDefinitionRegistrationFailed> failures)
    : Exception(
        $"Failed to register projection(s) {string.Join(", ", failures.Keys.Select(identifier => $"'{identifier}'"))}. " +
        $"Every other projection in the batch was registered. First failure: {failures.Values.First().GetBaseException().Message}",
        failures.Values.First())
{
    /// <summary>
    /// Gets the failures keyed by projection identifier.
    /// </summary>
    [Id(0)]
    public IReadOnlyDictionary<ProjectionId, ProjectionDefinitionRegistrationFailed> Failures { get; } = failures;

    /// <summary>
    /// Try to find all attributed projection registration failures in an exception tree.
    /// </summary>
    /// <param name="exception">The exception tree to inspect.</param>
    /// <param name="failures">The failures, if found.</param>
    /// <returns>True when attributed failures were found; otherwise false.</returns>
    public static bool TryFindFailures(
        Exception? exception,
        [NotNullWhen(true)] out IReadOnlyDictionary<ProjectionId, ProjectionDefinitionRegistrationFailed>? failures)
    {
        var collectedFailures = new Dictionary<ProjectionId, ProjectionDefinitionRegistrationFailed>();
        if (TryCollectFailures(exception, collectedFailures) && collectedFailures.Count > 0)
        {
            failures = collectedFailures;
            return true;
        }

        failures = null;
        return false;
    }

    static bool TryCollectFailures(
        Exception? exception,
        IDictionary<ProjectionId, ProjectionDefinitionRegistrationFailed> failures)
    {
        switch (exception)
        {
            case null:
                return false;

            case ProjectionDefinitionsRegistrationFailed failed:
                foreach (var (identifier, failure) in failed.Failures)
                {
                    failures.TryAdd(identifier, failure);
                }

                return true;

            case AggregateException aggregate:
                return aggregate.InnerExceptions.Count > 0 &&
                    aggregate.InnerExceptions.All(innerException => TryCollectFailures(innerException, failures));

            default:
                return TryCollectFailures(exception.InnerException, failures);
        }
    }
}
