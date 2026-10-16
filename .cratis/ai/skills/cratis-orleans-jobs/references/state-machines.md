<!-- cratis-ai-managed: skills/cratis-orleans-jobs/references/state-machines.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Grain lifecycle state machines

Source: `Cratis/Orleans`. Use explicit states when entering/leaving a status has behavior; an enum is enough for a status with no behavior. Do not put long-running work on a grain turn just because `OnEnter` is available.

## Stored lifecycle

`StateMachine<TStoredState>` derives from a stateful grain. Override `InitialState` and `CreateStates()`; states derive `State<TStoredState>`. `AllowedTransitions` is an immutable list of target types, or override `CanTransitionTo<TTargetState>(storedState)` for a guard (`Source/Orleans/StateMachines/StateMachine.cs`; `State.cs`).

Declaration excerpt for an existing grain file with `ProcessState : StateMachineState`, the existing `IProcess` grain interface, and a registered `processes` grain storage provider:

```csharp
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Cratis.Orleans.StateMachines;
using Orleans;
using Orleans.Providers;

[GrainType("my-product-process")]
[StorageProvider(ProviderName = "processes")]
public class Process : StateMachine<ProcessState>, IProcess
{
    protected override Type InitialState => typeof(Pending);

    public override IImmutableList<IState<ProcessState>> CreateStates() =>
        [new Pending(), new Finished()];

    public Task Finish() => TransitionTo<Finished>();
}

public class Pending : State<ProcessState>
{
    protected override IImmutableList<Type> AllowedTransitions => [typeof(Finished)];
}

public class Finished : State<ProcessState>;
```

Split types into their house-style files in the consumer; the block collects declarations to show the API. Do not copy the `processes` provider name into Chronicle's kernel; use its registered constants.

A refused transition is a **no-op**, not an exception or a successful change. An unknown target throws `UnknownStateTypeInStateMachine`. The machine runs leaving hooks, `OnLeave`, entering hooks, `OnEnter`, then persistence (`StateMachine.cs`). Transitions requested while entering are scheduled; transitions while leaving throw `TransitioningDuringOnLeaveIsNotSupported`.

When stored state derives `StateMachineState`, `CurrentState` stores the state's full type name; activation resolves it, enters it again, and can repeat enter effects. Renames need a stored-value migration; an unknown recorded name throws `UnknownCurrentState` (`StateMachine.cs`). A self-transition with the **same state instance** skips persistence: return a new stored-state instance when a self-transition must write.

For externally owned storage, the protected constructor accepts `Orleans.Core.IStorage<TStoredState>` and reads it on activation before the activation hook. Override `ResolveActivationState()` to derive lifecycle from authoritative state instead of persisting a second status.

## Lifecycle following Chronicle facts

`EventSourcedStateMachine<TModel>` from `Cratis.Orleans.Chronicle.StateMachines` uses a Chronicle read model as authoritative state. Its interface derives `IEventSourcedStateMachine` (string-keyed). There is **no grain storage provider** to configure and writes are no-ops: `ReadModelStorage<TModel>` reads Chronicle; `ResolveState(TModel?)` derives activation state (`Source/Orleans.Chronicle/StateMachines/EventSourcedStateMachine.cs`; `ReadModelStorage.cs`).

- Define states with `CreateStates()` as above, over `TModel`.
- Define event transitions in `DefineTransitions(IStateMachineTransitions<TModel>)`, using `.On<TEvent>().When(model => predicate).TransitionTo<TState>()`. First applicable allowed transition wins. Make `ResolveState` agree with these transitions after restart (`EventSourcedStateMachine.cs`, `StateMachineTransitions.cs`).
- Use model-bound projection attributes, an already-discovered projection, or `DefineProjection(IProjectionBuilderFor<TModel>)`, not competing projections. `DefineProjection` and `DefineTransitions` run at startup **without a constructor**: use only the builder, never injected services or fields (`EventSourcedStateMachine.cs`).
- Register `AddEventSourcedStateMachines()` on `ISiloBuilder` or `IServiceCollection`, beside the application's Chronicle client registration; namespace `Cratis.Orleans.Chronicle.Hosting` (`Hosting/EventSourcedStateMachinesServiceCollectionExtensions.cs`).
- Grain keys include event store, namespace and event-source ID. Use `IGrainFactory.GetEventSourcedStateMachine<TInterface>(eventStore, eventSourceId)` from `Cratis.Orleans.Chronicle.StateMachines` so tenant coordinates come from the supplied event store (`StateMachines/EventSourcedStateMachineGrainFactoryExtensions.cs`).
- Prefer passive models. For events consumed by a materialized projection, the reactor waits for its sequence position before refreshing; `ModelCatchUpTimeout` defaults to 30 seconds and expiry fails with `ModelDidNotCatchUp` (`EventSourcedStateMachineReactor.cs`; `EventSourcedStateMachinesOptions.cs`).

This machine **follows facts**, it does not arbitrate whether a command may append one. Keep command rejection and append-time invariants at their owning boundary. Enter/model-change effects may repeat on activation, redelivery or replay: design them idempotently (`Documentation/concepts/event-sourced-state-machines.md`). Test guards, no-op refusals, reactivation and persistence failures separately from job recovery.
