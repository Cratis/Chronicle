<!-- cratis-ai-managed: skills/cratis-chronicle-reactor/references/durable-side-effects.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Durable provider side effects

## Contents

- When it applies
- Recipe
- Illustrative handler
- Retry specification
- Related guidance

## When it applies

Use this pattern for an after-commit email, payment or webhook that must survive
crashes and retries. A stable delivery identity is not an exactly-once guarantee.

Command operations are inline: their journal is in memory, with no durable retry.
A reactor retry can execute its command's operations again. Chronicle outbox/inbox
moves public events **between event stores** (`EventSequenceId.Outbox` to the
consumer's inbox); it is not a per-effect job queue for provider calls.

## Recipe

1. Let the command's `Handle()` return the domain event. That committed fact is
   the durable trigger; do not append a signal/tick event just to wake a reactor.
2. Handle the fact in a reactor with an explicit `[Reactor("confirmation-email")]`
   identity. Its id contributes to the persisted delivery key, so renaming the
   class must not silently create a different observer identity.
3. Accept `ReactorDelivery delivery` and pass
   `delivery.IdempotencyKeyFor("confirmation-email").Value` as the provider's
   idempotency key. This is a 64-character hash including the effect name and
   epoch; do not send raw `delivery.Id` outside your boundary, and check the
   provider's key-length limit.
4. Require receiver-side deduplication on that key, including suitable retention
   and retry rules. For an effect in an application-owned database, write the
   receipt and effect in the same transaction. A separate receipt cannot make a
   remote provider call atomic.
5. On success, return a past-tense sent-fact event, such as `ConfirmationSent`.
   Returned events get idempotent append keys derived from the delivery. On a
   kernel supporting idempotent appends, repeating the same key and output does
   not append another copy. Keep output stable; do not generate fresh ids or timestamps.
6. Keep `[OnceOnly]` when replay must not repeat the effect. It excludes replay,
   not recovery retries, and does not replace provider-side idempotency.

**Failure window:** a crash after the provider accepts the request but before the
sent-fact is recorded causes another call on recovery. Only the provider's
idempotency contract makes that remote retry safe. A receipt written before the
call can instead mark work complete that was never sent. Never promise exactly-once.

## Illustrative handler

This excerpt uses `Cratis.Chronicle.Reactors` and `Cratis.Chronicle.Events`.
The application supplies `IConfirmationProvider.Send(ConfirmationRequest request,
string idempotencyKey)` returning `Task`, `ConfirmationRequested` with a `Request`
property, and the event `ConfirmationSent`. Here `ConfirmationRequested` records a
real user request; it is not a synthetic signal appended solely for this reactor.

```csharp
/// <summary>Sends a requested confirmation with receiver-side deduplication.</summary>
/// <param name="provider">The idempotent confirmation provider.</param>
[Reactor("confirmation-email")]
public class SendConfirmation(IConfirmationProvider provider) : IReactor
{
    /// <summary>Sends the confirmation and returns the sent fact.</summary>
    /// <param name="@event">The recorded request.</param>
    /// <param name="context">The source event context.</param>
    /// <param name="delivery">The stable delivery identity.</param>
    /// <returns>The sent fact for the source event.</returns>
    [OnceOnly]
    public async Task<ConfirmationSent> Send(
        ConfirmationRequested @event, EventContext context, ReactorDelivery delivery)
    {
        await provider.Send(@event.Request,
            idempotencyKey: delivery.IdempotencyKeyFor("confirmation-email").Value);
        return new ConfirmationSent();
    }
}
```

## Retry specification

Requires Chronicle 19.61.0 for `History` and `Redeliver`. Check the testing package
version; on older packages call the handler with the same context/delivery or
specify the receipt collaborator, as described in [specifications.md](specifications.md).

Use `ReactorScenario<SendConfirmation>` (`Cratis.Chronicle.Testing.Reactors`) with
an injected, deduplicating provider fake. The fake records every key, sends only
for a new key, then throws on the first call **after accepting the send**. On a
repeated key it returns successfully without sending again. This models the
failure window between the provider accepting and the handler returning its fact.

1. Record one occurrence without invoking the handler:
   `var occurrence = await scenario.Given.ForEventSource(id).History(@event);`.
2. Capture the exception from `await scenario.Redeliver(occurrence);`. Handler
   exceptions surface to the caller. Assert the fake accepted one send, and use
   `scenario.ShouldNotHaveProduced<ConfirmationSent>()` before any successful retry.
3. Call `await scenario.Redeliver(occurrence);` again. Assert both provider calls
   carried the same `delivery.IdempotencyKeyFor("confirmation-email").Value`,
   the fake sent once in total, and `scenario.ShouldHaveProduced<ConfirmationSent>()`.
4. Redeliver that same occurrence a third time. All keys must still match, with
   one fake-provider send. Do not use `Given.Events` twice: that creates distinct
   occurrences rather than preserving the original `ReactorDelivery` identity.

`ReactorScenario` records **every returned event**, including another sent-fact on
successful redelivery. `ShouldHaveProduced` checks presence, not a count. The
scenario does not append those events and does **not** prove durable append
deduplication. Returned-event append keys protect the event-log write only with a
kernel supporting idempotent appends; they do not stop the handler or provider
call from running again.

The scenario does not execute external effects for the test, prove observer
registration or prove retry scheduling. The injected fake supplies the provider
behavior; production startup checks belong to **cratis-application-slice-conformance**.
Run the reactor's specification project and require the retry assertions to pass.

## Related guidance

- Read [cratis-arc-command-operation](../../cratis-arc-command-operation/SKILL.md) for inline work and compensation, not durable delivery.
- Read [cratis-multi-service-architecture](../../cratis-multi-service-architecture/SKILL.md) for cross-store outbox/inbox routing.
- Read [specifications.md](specifications.md) for scenario setup and redelivery.
- Read [reactors.md](../../../rules/reactors.md) for returned commands and deliberate execution identity.
