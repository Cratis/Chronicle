// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;

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
    public static void ShouldBeFailed(this AppendedEventWithResult appendedEvent) =>
        appendedEvent.Result.ShouldBeFailed();

    /// <summary>
    /// Asserts that the <see cref="AppendedEventWithResult"/> has at least one constraint violation.
    /// </summary>
    /// <param name="appendedEvent">The <see cref="AppendedEventWithResult"/> to assert on.</param>
    public static void ShouldHaveConstraintViolations(this AppendedEventWithResult appendedEvent) =>
        appendedEvent.Result.ShouldHaveConstraintViolations();

    /// <summary>
    /// Asserts that the <see cref="AppendedEventWithResult"/> has no constraint violations.
    /// </summary>
    /// <param name="appendedEvent">The <see cref="AppendedEventWithResult"/> to assert on.</param>
    public static void ShouldNotHaveConstraintViolations(this AppendedEventWithResult appendedEvent) =>
        appendedEvent.Result.ShouldNotHaveConstraintViolations();

    /// <summary>
    /// Asserts that the <see cref="AppendedEventWithResult"/> has a constraint violation with the given name.
    /// </summary>
    /// <param name="appendedEvent">The <see cref="AppendedEventWithResult"/> to assert on.</param>
    /// <param name="constraintName">The expected <see cref="ConstraintName"/>.</param>
    public static void ShouldHaveConstraintViolationFor(this AppendedEventWithResult appendedEvent, ConstraintName constraintName) =>
        appendedEvent.Result.ShouldHaveConstraintViolationFor(constraintName);

    /// <summary>
    /// Asserts that the <see cref="AppendedEventWithResult"/> has at least one concurrency violation.
    /// </summary>
    /// <param name="appendedEvent">The <see cref="AppendedEventWithResult"/> to assert on.</param>
    public static void ShouldHaveConcurrencyViolations(this AppendedEventWithResult appendedEvent) =>
        appendedEvent.Result.ShouldHaveConcurrencyViolations();

    /// <summary>
    /// Asserts that the <see cref="AppendedEventWithResult"/> has no concurrency violations.
    /// </summary>
    /// <param name="appendedEvent">The <see cref="AppendedEventWithResult"/> to assert on.</param>
    public static void ShouldNotHaveConcurrencyViolations(this AppendedEventWithResult appendedEvent) =>
        appendedEvent.Result.ShouldNotHaveConcurrencyViolations();

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
    /// <exception cref="AppendResultAssertionException">Thrown when the event content is not of the expected type.</exception>
    public static void ShouldHaveEvent<TEvent>(this AppendedEventWithResult appendedEvent, Action<TEvent>? validate = null)
    {
        if (appendedEvent.Event.Content is not TEvent content)
        {
            throw new AppendResultAssertionException(
                $"Expected event content to be of type '{typeof(TEvent).Name}', but was '{appendedEvent.Event.Content?.GetType().Name ?? "null"}'.");
        }

        validate?.Invoke(content);
    }

    /// <summary>
    /// Asserts that the event content of the <see cref="AppendedEventWithResult"/> is for the given event source.
    /// </summary>
    /// <param name="appendedEvent">The <see cref="AppendedEventWithResult"/> to assert on.</param>
    /// <param name="eventSourceId">The expected <see cref="EventSourceId"/>.</param>
    /// <exception cref="AppendResultAssertionException">Thrown when the event source does not match.</exception>
    public static void ShouldBeForEventSource(this AppendedEventWithResult appendedEvent, EventSourceId eventSourceId)
    {
        if (appendedEvent.Event.Context.EventSourceId != eventSourceId)
        {
            throw new AppendResultAssertionException(
                $"Expected event source '{eventSourceId}', but was '{appendedEvent.Event.Context.EventSourceId}'.");
        }
    }
}
