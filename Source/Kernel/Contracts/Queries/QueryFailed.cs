// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Validation;

namespace Cratis.Chronicle.Contracts.Queries;

/// <summary>
/// The exception that is thrown when a query performed against the kernel did not succeed.
/// </summary>
/// <param name="validationResults">The validation results for the query.</param>
/// <param name="exceptionMessages">Any exception messages that occurred.</param>
public class QueryFailed(IEnumerable<ValidationResult> validationResults, IEnumerable<string> exceptionMessages)
    : Exception(BuildMessage(validationResults, exceptionMessages))
{
    /// <summary>
    /// Gets the validation results for the query.
    /// </summary>
    public IList<ValidationResult> ValidationResults { get; } = [.. validationResults];

    /// <summary>
    /// Gets any exception messages that occurred.
    /// </summary>
    public IList<string> ExceptionMessages { get; } = [.. exceptionMessages];

    static string BuildMessage(IEnumerable<ValidationResult> validationResults, IEnumerable<string> exceptionMessages) =>
        $"Query failed: {string.Join(", ", validationResults.Select(_ => _.Message).Concat(exceptionMessages))}";
}
