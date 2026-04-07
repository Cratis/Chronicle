// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Xunit;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Should-style assertion extensions for <see cref="AppendedEventWithResult"/>.
/// </summary>
public static class AppendedEventWithResultShouldExtensions
{
    /// <summary>
    /// Asserts that the <see cref="AppendedEventWithResult"/> is successful.
    /// </summary>
    /// <param name="appendedEvent">The <see cref="AppendedEventWithResult"/> to assert on.</param>
    public static void ShouldBeSuccessful(this AppendedEventWithResult appendedEvent) =>
        appendedEvent.Result.ShouldBeSuccessful();

    /// <summary>
    /// Asserts that the <see cref="AppendedEventWithResult"/> is not successful.
    /// </summary>
    /// <param name="appendedEvent">The <see cref="AppendedEventWithResult"/> to assert on.</param>
    public static void ShouldNotBeSuccessful(this AppendedEventWithResult appendedEvent) =>
        appendedEvent.Result.ShouldNotBeSuccessful();

    /// <summary>
    /// Asserts that the <see cref="AppendedEventWithResult"/> has at least one constraint violation.
    /// </summary>
    /// <param name="appendedEvent">The <see cref="AppendedEventWithResult"/> to assert on.</param>
    public static void ShouldHaveConstraintViolation(this AppendedEventWithResult appendedEvent) =>
        appendedEvent.Result.ShouldHaveConstraintViolation();

    /// <summary>
    /// Asserts that the <see cref="AppendedEventWithResult"/> has no constraint violations.
    /// </summary>
    /// <param name="appendedEvent">The <see cref="AppendedEventWithResult"/> to assert on.</param>
    public static void ShouldNotHaveConstraintViolation(this AppendedEventWithResult appendedEvent) =>
        appendedEvent.Result.ShouldNotHaveConstraintViolation();

    /// <summary>
    /// Asserts that the <see cref="AppendedEventWithResult"/> has a constraint violation with the given name.
    /// </summary>
    /// <param name="appendedEvent">The <see cref="AppendedEventWithResult"/> to assert on.</param>
    /// <param name="constraintName">The expected <see cref="ConstraintName"/>.</param>
    public static void ShouldHaveConstraintViolation(this AppendedEventWithResult appendedEvent, ConstraintName constraintName) =>
        appendedEvent.Result.ShouldHaveConstraintViolation(constraintName);

    /// <summary>
    /// Asserts that the <see cref="AppendedEventWithResult"/> has a concurrency violation.
    /// </summary>
    /// <param name="appendedEvent">The <see cref="AppendedEventWithResult"/> to assert on.</param>
    public static void ShouldHaveConcurrencyViolation(this AppendedEventWithResult appendedEvent) =>
        appendedEvent.Result.ShouldHaveConcurrencyViolation();

    /// <summary>
    /// Asserts that the <see cref="AppendedEventWithResult"/> has no concurrency violation.
    /// </summary>
    /// <param name="appendedEvent">The <see cref="AppendedEventWithResult"/> to assert on.</param>
    public static void ShouldNotHaveConcurrencyViolation(this AppendedEventWithResult appendedEvent) =>
        appendedEvent.Result.ShouldNotHaveConcurrencyViolation();

    /// <summary>
    /// Asserts that the <see cref="AppendedEventWithResult"/> has errors.
    /// </summary>
    /// <param name="appendedEvent">The <see cref="AppendedEventWithResult"/> to assert on.</param>
    public static void ShouldHaveErrors(this AppendedEventWithResult appendedEvent) =>
        appendedEvent.Result.ShouldHaveErrors();

    /// <summary>
    /// Asserts that the <see cref="AppendedEventWithResult"/> has no errors.
    /// </summary>
    /// <param name="appendedEvent">The <see cref="AppendedEventWithResult"/> to assert on.</param>
    public static void ShouldNotHaveErrors(this AppendedEventWithResult appendedEvent) =>
        appendedEvent.Result.ShouldNotHaveErrors();

    /// <summary>
    /// Asserts that the event content of the <see cref="AppendedEventWithResult"/> is of the given type and optionally validates it.
    /// </summary>
    /// <typeparam name="TEvent">The expected event type.</typeparam>
    /// <param name="appendedEvent">The <see cref="AppendedEventWithResult"/> to assert on.</param>
    /// <param name="validate">Optional action to further validate the event content.</param>
    public static void ShouldHaveEvent<TEvent>(this AppendedEventWithResult appendedEvent, Action<TEvent>? validate = null)
    {
        Assert.IsType<TEvent>(appendedEvent.Event.Content);
        validate?.Invoke((TEvent)appendedEvent.Event.Content);
    }

    /// <summary>
    /// Asserts that the event content of the <see cref="AppendedEventWithResult"/> is for the given event source.
    /// </summary>
    /// <param name="appendedEvent">The <see cref="AppendedEventWithResult"/> to assert on.</param>
    /// <param name="eventSourceId">The expected <see cref="EventSourceId"/>.</param>
    public static void ShouldBeForEventSource(this AppendedEventWithResult appendedEvent, EventSourceId eventSourceId) =>
        Assert.Equal(eventSourceId, appendedEvent.Event.Context.EventSourceId);
}
