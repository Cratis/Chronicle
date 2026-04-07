// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Provides assertion extensions for <see cref="IAppendResult"/>, <see cref="AppendResult"/>, and <see cref="AppendManyResult"/>.
/// </summary>
public static class AppendResultShouldExtensions
{
    /// <summary>
    /// Asserts that the result represents a successful operation.
    /// </summary>
    /// <param name="result">The result to assert on.</param>
    /// <exception cref="AppendResultAssertionException">Thrown when the result is not successful.</exception>
    public static void ShouldBeSuccessful(this IAppendResult result)
    {
        if (result.IsSuccess)
        {
            return;
        }

        var message = new StringBuilder("Expected append result to be successful, but it failed.");

        if (result.HasConstraintViolations)
        {
            message.AppendLine().Append("Constraint violations:");
            foreach (var violation in result.ConstraintViolations)
            {
                message.AppendLine().Append($"  [{violation.ConstraintType}] {violation.ConstraintName}: {violation.Message}");
            }
        }

        if (result.HasErrors)
        {
            message.AppendLine().Append("Errors:");
            foreach (var error in result.Errors)
            {
                message.AppendLine().Append($"  {error}");
            }
        }

        if (result.HasConcurrencyViolations)
        {
            message.AppendLine().Append("Concurrency violations occurred.");
        }

        throw new AppendResultAssertionException(message.ToString());
    }

    /// <summary>
    /// Asserts that the result represents a failed operation.
    /// </summary>
    /// <param name="result">The result to assert on.</param>
    /// <exception cref="AppendResultAssertionException">Thrown when the result is successful.</exception>
    public static void ShouldBeFailed(this IAppendResult result)
    {
        if (!result.IsSuccess)
        {
            return;
        }

        throw new AppendResultAssertionException("Expected append result to be failed, but it was successful.");
    }

    /// <summary>
    /// Asserts that the result has at least one constraint violation.
    /// </summary>
    /// <param name="result">The result to assert on.</param>
    /// <exception cref="AppendResultAssertionException">Thrown when there are no constraint violations.</exception>
    public static void ShouldHaveConstraintViolations(this IAppendResult result)
    {
        if (result.HasConstraintViolations)
        {
            return;
        }

        throw new AppendResultAssertionException("Expected append result to have constraint violations, but none were present.");
    }

    /// <summary>
    /// Asserts that the result has no constraint violations.
    /// </summary>
    /// <param name="result">The result to assert on.</param>
    /// <exception cref="AppendResultAssertionException">Thrown when constraint violations are present.</exception>
    public static void ShouldNotHaveConstraintViolations(this IAppendResult result)
    {
        if (!result.HasConstraintViolations)
        {
            return;
        }

        var message = new StringBuilder("Expected append result to have no constraint violations, but found:");
        foreach (var violation in result.ConstraintViolations)
        {
            message.AppendLine().Append($"  [{violation.ConstraintType}] {violation.ConstraintName}: {violation.Message}");
        }

        throw new AppendResultAssertionException(message.ToString());
    }

    /// <summary>
    /// Asserts that the result has at least one constraint violation for a specific <see cref="ConstraintName"/>.
    /// </summary>
    /// <param name="result">The result to assert on.</param>
    /// <param name="constraintName">The <see cref="ConstraintName"/> to look for.</param>
    /// <exception cref="AppendResultAssertionException">Thrown when the expected constraint violation is not present.</exception>
    public static void ShouldHaveConstraintViolationFor(this IAppendResult result, ConstraintName constraintName)
    {
        if (result.ConstraintViolations.Any(v => v.ConstraintName == constraintName))
        {
            return;
        }

        var message = new StringBuilder($"Expected append result to have a constraint violation for '{constraintName}', but none was found.");
        if (result.HasConstraintViolations)
        {
            message.AppendLine().Append("Actual constraint violations:");
            foreach (var violation in result.ConstraintViolations)
            {
                message.AppendLine().Append($"  [{violation.ConstraintType}] {violation.ConstraintName}: {violation.Message}");
            }
        }

        throw new AppendResultAssertionException(message.ToString());
    }

    /// <summary>
    /// Asserts that the result has at least one concurrency violation.
    /// </summary>
    /// <param name="result">The result to assert on.</param>
    /// <exception cref="AppendResultAssertionException">Thrown when there are no concurrency violations.</exception>
    public static void ShouldHaveConcurrencyViolations(this IAppendResult result)
    {
        if (result.HasConcurrencyViolations)
        {
            return;
        }

        throw new AppendResultAssertionException("Expected append result to have concurrency violations, but none were present.");
    }

    /// <summary>
    /// Asserts that the result has no concurrency violations.
    /// </summary>
    /// <param name="result">The result to assert on.</param>
    /// <exception cref="AppendResultAssertionException">Thrown when concurrency violations are present.</exception>
    public static void ShouldNotHaveConcurrencyViolations(this IAppendResult result)
    {
        if (!result.HasConcurrencyViolations)
        {
            return;
        }

        throw new AppendResultAssertionException("Expected append result to have no concurrency violations, but some were present.");
    }

    /// <summary>
    /// Asserts that the result has at least one error.
    /// </summary>
    /// <param name="result">The result to assert on.</param>
    /// <exception cref="AppendResultAssertionException">Thrown when there are no errors.</exception>
    public static void ShouldHaveErrors(this IAppendResult result)
    {
        if (result.HasErrors)
        {
            return;
        }

        throw new AppendResultAssertionException("Expected append result to have errors, but none were present.");
    }

    /// <summary>
    /// Asserts that the result has no errors.
    /// </summary>
    /// <param name="result">The result to assert on.</param>
    /// <exception cref="AppendResultAssertionException">Thrown when errors are present.</exception>
    public static void ShouldNotHaveErrors(this IAppendResult result)
    {
        if (!result.HasErrors)
        {
            return;
        }

        var message = new StringBuilder("Expected append result to have no errors, but found:");
        foreach (var error in result.Errors)
        {
            message.AppendLine().Append($"  {error}");
        }

        throw new AppendResultAssertionException(message.ToString());
    }
}
