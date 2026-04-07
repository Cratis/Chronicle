// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Xunit;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Should-style assertion extensions for <see cref="AppendResult"/> and <see cref="AppendManyResult"/>.
/// </summary>
public static class AppendResultShouldExtensions
{
    /// <summary>
    /// Asserts that the <see cref="AppendResult"/> is successful.
    /// </summary>
    /// <param name="result">The <see cref="AppendResult"/> to assert on.</param>
    public static void ShouldBeSuccessful(this AppendResult result) =>
        Assert.True(result.IsSuccess, $"Expected append to succeed, but it failed. Constraint violations: [{string.Join(", ", result.ConstraintViolations)}]. Errors: [{string.Join(", ", result.Errors)}].");

    /// <summary>
    /// Asserts that the <see cref="AppendResult"/> is not successful.
    /// </summary>
    /// <param name="result">The <see cref="AppendResult"/> to assert on.</param>
    public static void ShouldNotBeSuccessful(this AppendResult result) =>
        Assert.False(result.IsSuccess, "Expected append to fail, but it succeeded.");

    /// <summary>
    /// Asserts that the <see cref="AppendResult"/> has at least one constraint violation.
    /// </summary>
    /// <param name="result">The <see cref="AppendResult"/> to assert on.</param>
    public static void ShouldHaveConstraintViolation(this AppendResult result) =>
        Assert.True(result.HasConstraintViolations, "Expected append result to have constraint violations, but it did not.");

    /// <summary>
    /// Asserts that the <see cref="AppendResult"/> has no constraint violations.
    /// </summary>
    /// <param name="result">The <see cref="AppendResult"/> to assert on.</param>
    public static void ShouldNotHaveConstraintViolation(this AppendResult result) =>
        Assert.False(result.HasConstraintViolations, $"Expected append result to have no constraint violations, but found: [{string.Join(", ", result.ConstraintViolations)}].");

    /// <summary>
    /// Asserts that the <see cref="AppendResult"/> has a constraint violation with the given name.
    /// </summary>
    /// <param name="result">The <see cref="AppendResult"/> to assert on.</param>
    /// <param name="constraintName">The expected <see cref="ConstraintName"/>.</param>
    public static void ShouldHaveConstraintViolation(this AppendResult result, ConstraintName constraintName) =>
        Assert.Contains(result.ConstraintViolations, v => v.ConstraintName == constraintName);

    /// <summary>
    /// Asserts that the <see cref="AppendResult"/> has a concurrency violation.
    /// </summary>
    /// <param name="result">The <see cref="AppendResult"/> to assert on.</param>
    public static void ShouldHaveConcurrencyViolation(this AppendResult result) =>
        Assert.True(result.HasConcurrencyViolations, "Expected append result to have a concurrency violation, but it did not.");

    /// <summary>
    /// Asserts that the <see cref="AppendResult"/> has no concurrency violation.
    /// </summary>
    /// <param name="result">The <see cref="AppendResult"/> to assert on.</param>
    public static void ShouldNotHaveConcurrencyViolation(this AppendResult result) =>
        Assert.False(result.HasConcurrencyViolations, "Expected append result to have no concurrency violation, but it did.");

    /// <summary>
    /// Asserts that the <see cref="AppendResult"/> has errors.
    /// </summary>
    /// <param name="result">The <see cref="AppendResult"/> to assert on.</param>
    public static void ShouldHaveErrors(this AppendResult result) =>
        Assert.True(result.HasErrors, "Expected append result to have errors, but it did not.");

    /// <summary>
    /// Asserts that the <see cref="AppendResult"/> has no errors.
    /// </summary>
    /// <param name="result">The <see cref="AppendResult"/> to assert on.</param>
    public static void ShouldNotHaveErrors(this AppendResult result) =>
        Assert.False(result.HasErrors, $"Expected append result to have no errors, but found: [{string.Join(", ", result.Errors)}].");

    /// <summary>
    /// Asserts that the <see cref="AppendManyResult"/> is successful.
    /// </summary>
    /// <param name="result">The <see cref="AppendManyResult"/> to assert on.</param>
    public static void ShouldBeSuccessful(this AppendManyResult result) =>
        Assert.True(result.IsSuccess, $"Expected append to succeed, but it failed. Constraint violations: [{string.Join(", ", result.ConstraintViolations)}]. Errors: [{string.Join(", ", result.Errors)}].");

    /// <summary>
    /// Asserts that the <see cref="AppendManyResult"/> is not successful.
    /// </summary>
    /// <param name="result">The <see cref="AppendManyResult"/> to assert on.</param>
    public static void ShouldNotBeSuccessful(this AppendManyResult result) =>
        Assert.False(result.IsSuccess, "Expected append to fail, but it succeeded.");

    /// <summary>
    /// Asserts that the <see cref="AppendManyResult"/> has at least one constraint violation.
    /// </summary>
    /// <param name="result">The <see cref="AppendManyResult"/> to assert on.</param>
    public static void ShouldHaveConstraintViolation(this AppendManyResult result) =>
        Assert.True(result.HasConstraintViolations, "Expected append result to have constraint violations, but it did not.");

    /// <summary>
    /// Asserts that the <see cref="AppendManyResult"/> has no constraint violations.
    /// </summary>
    /// <param name="result">The <see cref="AppendManyResult"/> to assert on.</param>
    public static void ShouldNotHaveConstraintViolation(this AppendManyResult result) =>
        Assert.False(result.HasConstraintViolations, $"Expected append result to have no constraint violations, but found: [{string.Join(", ", result.ConstraintViolations)}].");

    /// <summary>
    /// Asserts that the <see cref="AppendManyResult"/> has a constraint violation with the given name.
    /// </summary>
    /// <param name="result">The <see cref="AppendManyResult"/> to assert on.</param>
    /// <param name="constraintName">The expected <see cref="ConstraintName"/>.</param>
    public static void ShouldHaveConstraintViolation(this AppendManyResult result, ConstraintName constraintName) =>
        Assert.Contains(result.ConstraintViolations, v => v.ConstraintName == constraintName);

    /// <summary>
    /// Asserts that the <see cref="AppendManyResult"/> has at least one concurrency violation.
    /// </summary>
    /// <param name="result">The <see cref="AppendManyResult"/> to assert on.</param>
    public static void ShouldHaveConcurrencyViolation(this AppendManyResult result) =>
        Assert.True(result.HasConcurrencyViolations, "Expected append result to have concurrency violations, but it did not.");

    /// <summary>
    /// Asserts that the <see cref="AppendManyResult"/> has no concurrency violations.
    /// </summary>
    /// <param name="result">The <see cref="AppendManyResult"/> to assert on.</param>
    public static void ShouldNotHaveConcurrencyViolation(this AppendManyResult result) =>
        Assert.False(result.HasConcurrencyViolations, "Expected append result to have no concurrency violations, but it did.");

    /// <summary>
    /// Asserts that the <see cref="AppendManyResult"/> has errors.
    /// </summary>
    /// <param name="result">The <see cref="AppendManyResult"/> to assert on.</param>
    public static void ShouldHaveErrors(this AppendManyResult result) =>
        Assert.True(result.HasErrors, "Expected append result to have errors, but it did not.");

    /// <summary>
    /// Asserts that the <see cref="AppendManyResult"/> has no errors.
    /// </summary>
    /// <param name="result">The <see cref="AppendManyResult"/> to assert on.</param>
    public static void ShouldNotHaveErrors(this AppendManyResult result) =>
        Assert.False(result.HasErrors, $"Expected append result to have no errors, but found: [{string.Join(", ", result.Errors)}].");
}
