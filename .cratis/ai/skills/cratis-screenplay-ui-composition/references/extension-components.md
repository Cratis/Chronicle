<!-- cratis-ai-managed: skills/cratis-screenplay-ui-composition/references/extension-components.md -->
# Extension components

A screen places a component an [extension package](../../cratis-screenplay-extension-packages/SKILL.md) provides through the system that names the extension, never through the package: `component <System>.<Component> <name>`. Swapping a compatible provider changes nothing on the screen.

```play
screen Checkout
  component Payments.PaymentForm payment
    property amount = "10"
    on succeeded
      execute ConfirmPayment
        with paymentIntentId from signal.paymentIntentId
    on failed
      notify warning "The payment failed"
```

- `property` lines, `on <signal>` (camel case, a signal the component `raises`) and `signal.<value>` (a typed value the handled signal carries) are checked against the restored contract: `PLAY1114` unknown component, `PLAY1115` undeclared property, `PLAY1116` signal not raised, `PLAY1117` value the signal does not carry or no signal handled. `on <System>.<Trigger>` is checked against the contract's triggers (`PLAY1112`).
- At run time a handled signal is a Scene `SignalInteractionTrigger` (`{ "signalName": "succeeded" }`), resolved with `resolveSignalBehaviors`; it never fires an application `trigger` of the same name, and the reverse. Stage binds `on <signal>` to it when the name is a signal the component's contract declares, runs the behaviors with the Scene interaction engine when the component raises the signal, and reads `signal.<value>` from the values it raised. Do not model a signal as a `trigger`, and do not hand-wire a DOM event for it. See Stage [extension components](https://github.com/Cratis/Stage/blob/main/Documentation/extensions/components.md#handling-a-raised-signal) and Scene [binding a raised signal](https://github.com/Cratis/Scene/blob/main/Documentation/extensions/contract.md).
- A `ui profile` platform that the component's `platform` lines do not list is the warning `PLAY1118`.
- Nothing is checked until the contract is restored; an unrestored contract is reported where the system names the extension (`PLAY1108`).
- Read Screenplay [screens: extension components](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/screens.md#extension-components), [interaction triggers](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/interactions.md#interaction-triggers) and the [UI contract](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/ui-contract.md#extension-components) for exact placement. Stage's component hosting is in its [extensions documentation](https://github.com/Cratis/Stage/blob/main/Documentation/extensions/index.md); do not claim a component renders without a render and run.
