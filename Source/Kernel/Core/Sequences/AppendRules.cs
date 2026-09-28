// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Holds the validation rules shared by the append commands, so the plain and named-tag variants of each append
/// reject exactly the same requests.
/// </summary>
/// <remarks>
/// Each validator still declares its own <c language="csharp">RuleFor</c> expressions; only the rule and its message are shared. That
/// keeps the member paths the validators report - and that Arc's proxy generator mirrors - exactly as they were.
/// </remarks>
internal static class AppendRules
{
    /// <summary>
    /// Requires an event store name.
    /// </summary>
    /// <typeparam name="T">Type being validated.</typeparam>
    /// <typeparam name="TProperty">Type of the validated value; a concept is validated through its underlying value.</typeparam>
    /// <param name="rule">The rule builder.</param>
    /// <returns>The rule builder options for continuation.</returns>
    public static IRuleBuilderOptions<T, TProperty> RequiredEventStore<T, TProperty>(this IRuleBuilder<T, TProperty> rule) =>
        rule.NotEmpty().WithMessage("Event store name is required.");

    /// <summary>
    /// Requires a namespace name.
    /// </summary>
    /// <typeparam name="T">Type being validated.</typeparam>
    /// <typeparam name="TProperty">Type of the validated value; a concept is validated through its underlying value.</typeparam>
    /// <param name="rule">The rule builder.</param>
    /// <returns>The rule builder options for continuation.</returns>
    public static IRuleBuilderOptions<T, TProperty> RequiredNamespace<T, TProperty>(this IRuleBuilder<T, TProperty> rule) =>
        rule.NotEmpty().WithMessage("Namespace name is required.");

    /// <summary>
    /// Requires an event sequence identifier.
    /// </summary>
    /// <typeparam name="T">Type being validated.</typeparam>
    /// <typeparam name="TProperty">Type of the validated value; a concept is validated through its underlying value.</typeparam>
    /// <param name="rule">The rule builder.</param>
    /// <returns>The rule builder options for continuation.</returns>
    public static IRuleBuilderOptions<T, TProperty> RequiredEventSequence<T, TProperty>(this IRuleBuilder<T, TProperty> rule) =>
        rule.NotEmpty().WithMessage("Event sequence identifier is required.");

    /// <summary>
    /// Requires an event source identifier.
    /// </summary>
    /// <typeparam name="T">Type being validated.</typeparam>
    /// <typeparam name="TEventSourceId">Type of the event source identifier; a concept is validated through its underlying value.</typeparam>
    /// <param name="rule">The rule builder.</param>
    /// <returns>The rule builder options for continuation.</returns>
    public static IRuleBuilderOptions<T, TEventSourceId> RequiredEventSource<T, TEventSourceId>(this IRuleBuilder<T, TEventSourceId> rule) =>
        rule.NotEmpty().WithMessage("Event source identifier is required.");

    /// <summary>
    /// Requires at least one event in a batch. A missing batch is rejected the same way as an empty one.
    /// </summary>
    /// <typeparam name="T">Type being validated.</typeparam>
    /// <typeparam name="TEvent">Type of event in the batch.</typeparam>
    /// <param name="rule">The rule builder.</param>
    /// <returns>The rule builder options for continuation.</returns>
    public static IRuleBuilderOptions<T, IEnumerable<TEvent>> RequiredEvents<T, TEvent>(this IRuleBuilder<T, IEnumerable<TEvent>> rule) =>
        rule.NotEmpty().WithMessage("At least one event is required.");

    /// <summary>
    /// Requires an event type.
    /// </summary>
    /// <typeparam name="T">Type being validated.</typeparam>
    /// <param name="rule">The rule builder.</param>
    /// <returns>The rule builder options for continuation.</returns>
    public static IRuleBuilderOptions<T, EventType> RequiredEventType<T>(this IRuleBuilder<T, EventType> rule) =>
        rule.NotNull().WithMessage("Event type is required.");

    /// <summary>
    /// Requires an event type identifier, guarded through the event type itself.
    /// </summary>
    /// <typeparam name="T">Type being validated.</typeparam>
    /// <param name="rule">The rule builder.</param>
    /// <returns>The rule builder options for continuation.</returns>
    /// <remarks>
    /// Used where the rule is declared on the command itself: Arc's proxy generator mirrors a nested member rule
    /// such as <c language="csharp">RuleFor(_ => _.EventType.Id)</c> into TypeScript verbatim, emitting <c language="typescript">c.eventType.Id</c> - the
    /// un-camel-cased member on a possibly undefined object - which does not compile. A <c language="csharp">Must</c> is not mirrored
    /// at all, so the rule stays server-side, where it is the authority anyway.
    /// </remarks>
    public static IRuleBuilderOptions<T, EventType> RequiredEventTypeIdentifier<T>(this IRuleBuilder<T, EventType> rule) =>
        rule.Must(eventType => eventType is null || !string.IsNullOrEmpty(eventType.Id))
            .WithMessage("Event type identifier is required.");

    /// <summary>
    /// Requires an event type identifier on the identifier member itself. Used for per-event child rules.
    /// </summary>
    /// <typeparam name="T">Type being validated.</typeparam>
    /// <param name="rule">The rule builder.</param>
    /// <returns>The rule builder options for continuation.</returns>
    public static IRuleBuilderOptions<T, string> RequiredEventTypeId<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("Event type identifier is required.");

    /// <summary>
    /// Requires event content.
    /// </summary>
    /// <typeparam name="T">Type being validated.</typeparam>
    /// <param name="rule">The rule builder.</param>
    /// <returns>The rule builder options for continuation.</returns>
    public static IRuleBuilderOptions<T, string> RequiredContent<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotNull().WithMessage("Event content is required.");

    /// <summary>
    /// Requires every named tag to be present, to have a nonblank name and to have a value.
    /// </summary>
    /// <typeparam name="T">Type being validated.</typeparam>
    /// <param name="rule">The per-tag rule builder.</param>
    /// <returns>The rule builder options for continuation.</returns>
    /// <remarks>
    /// These mirror the invariants <see cref="Concepts.Events.NamedTag"/> enforces by throwing, so a malformed tag is
    /// reported as a validation error instead of failing the append with an exception. An empty value is allowed.
    /// </remarks>
    public static IRuleBuilderOptions<T, NamedTag> ValidNamedTag<T>(this IRuleBuilder<T, NamedTag> rule) =>
        rule.NotNull().WithMessage("Named tag is required.")
            .ChildRules(tag =>
            {
                tag.RuleFor(_ => _.Name).NotEmpty().WithMessage("Named tag name is required.");
                tag.RuleFor(_ => _.Value).NotNull().WithMessage("Named tag value is required.");
            });
}
