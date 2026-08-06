// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Validation;

namespace Cratis.Chronicle.Contracts.Queries;

/// <summary>
/// The exception that is thrown when a query performed against the kernel did not succeed.
/// </summary>
/// <param name="validationResults">The validation results for the query.</param>
/// <param name="exceptionMessages">Any exception messages that occurred.</param>
/// <param name="exceptionStackTrace">The stack trace captured where the query failed, if any.</param>
public class QueryFailed(IEnumerable<ValidationResult> validationResults, IEnumerable<string> exceptionMessages, string exceptionStackTrace = "")
    : Exception(BuildMessage(validationResults, exceptionMessages, exceptionStackTrace))
{
    /// <summary>
    /// Gets the validation results for the query.
    /// </summary>
    public IList<ValidationResult> ValidationResults { get; } = [.. validationResults];

    /// <summary>
    /// Gets any exception messages that occurred.
    /// </summary>
    public IList<string> ExceptionMessages { get; } = [.. exceptionMessages];

    /// <summary>
    /// Gets the stack trace captured where the query failed.
    /// </summary>
    /// <remarks>
    /// The failure happened on the other side of the wire, so this exception's own stack trace only shows
    /// the caller. Carrying the originating one is the only way to see where the query actually broke.
    /// </remarks>
    public string ExceptionStackTrace { get; } = exceptionStackTrace;

    static string BuildMessage(IEnumerable<ValidationResult> validationResults, IEnumerable<string> exceptionMessages, string exceptionStackTrace)
    {
        var message = $"Query failed: {string.Join(", ", validationResults.Select(_ => _.Message).Concat(exceptionMessages))}";
        return string.IsNullOrEmpty(exceptionStackTrace) ? message : $"{message}{Environment.NewLine}{exceptionStackTrace}";
    }
}
