<!-- cratis-ai-managed: skills/cratis-orleans-jobs/references/state-machines.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Grain lifecycle state machines

Verified from **Cratis/Orleans v1.16.3**. Use explicit states when entering/leaving a status has behavior; an enum is enough for a status with no behavior. Do not put long-running work on a grain turn just because `OnEnter` is available.

## Stored lifecycle

`StateMachine<TStoredState>` derives from a stateful grain. Override `InitialState` and `CreateStates()`; states derive `State<TStoredState>`. `AllowedTransitions` is an immutable list of target types, or override `CanTransitionTo<TTargetState>(storedState)` for a guard (`Source/Orleans/StateMachines/StateMachine.cs:63-67,120-192`; `State.cs:17-34`).

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

A refused transition is a **no-op**, not an exception or a successful change. An unknown target throws `UnknownStateTypeInStateMachine`. The machine runs leaving hooks, `OnLeave`, entering hooks, `OnEnter`, then persistence (`StateMachine.cs:162-169,222-290`). Transitions requested while entering are scheduled; transitions while leaving throw `TransitioningDuringOnLeaveIsNotSupported`.

When stored state derives `StateMachineState`, `CurrentState` stores the state's full type name; activation resolves it, enters it again, and can repeat enter effects. Renames need a stored-value migration; an unknown recorded name throws `UnknownCurrentState` (`StateMachine.cs:183-191,261-264`). A self-transition with the **same state instance** skips persistence: return a new stored-state instance when a self-transition must write (`:266-272`).

For externally owned storage, the protected constructor accepts `Orleans.Core.IStorage<TStoredState>` and reads it on activation before the activation hook. Override `ResolveActivationState()` to derive lifecycle from authoritative state instead of persisting a second status (`:39-51,68-95,172-192`).

## Lifecycle following Chronicle facts

`EventSourcedStateMachine<TModel>` from `Cratis.Orleans.Chronicle.StateMachines` uses a Chronicle read model as authoritative state. Its interface derives `IEventSourcedStateMachine` (string-keyed). There is **no grain storage provider** to configure and writes are no-ops: `ReadModelStorage<TModel>` reads Chronicle; `ResolveState(TModel?)` derives activation state (`Source/Orleans.Chronicle/StateMachines/EventSourcedStateMachine.cs:19-40,77-94,155-176`; `ReadModelStorage.cs:68-95`).

- Define states with `CreateStates()` as above, over `TModel`.
- Define event transitions in `DefineTransitions(IStateMachineTransitions<TModel>)`, using `.On<TEvent>().When(model => predicate).TransitionTo<TState>()`. First applicable allowed transition wins. Make `ResolveState` agree with these transitions after restart (`EventSourcedStateMachine.cs:142-176`, `StateMachineTransitions.cs`).
- Use model-bound projection attributes, an already-discovered projection, or `DefineProjection(IProjectionBuilderFor<TModel>)`, not competing projections. `DefineProjection` and `DefineTransitions` run at startup **without a constructor**: use only the builder, never injected services or fields (`EventSourcedStateMachine.cs:34-37`).
- Register `AddEventSourcedStateMachines()` on `ISiloBuilder` or `IServiceCollection`, beside the application's Chronicle client registration; namespace `Cratis.Orleans.Chronicle.Hosting` (`Hosting/EventSourcedStateMachinesServiceCollectionExtensions.cs:30-55`).
- Grain keys include event store, namespace and event-source ID. Use `IGrainFactory.GetEventSourcedStateMachine<TInterface>(eventStore, eventSourceId)` from `Cratis.Orleans.Chronicle.StateMachines` so tenant coordinates come from the supplied event store (`StateMachines/EventSourcedStateMachineGrainFactoryExtensions.cs:21-52`).
- Prefer passive models. For events consumed by a materialized projection, the reactor waits for its sequence position before refreshing; `ModelCatchUpTimeout` defaults to 30 seconds and expiry fails with `ModelDidNotCatchUp` (`EventSourcedStateMachineReactor.cs:58-89`; `EventSourcedStateMachinesOptions.cs:26`).

This machine **follows facts**, it does not arbitrate whether a command may append one. Keep command rejection and append-time invariants at their owning boundary. Enter/model-change effects may repeat on activation, redelivery or replay: design them idempotently (`Documentation/concepts/event-sourced-state-machines.md:240-247`). Test guards, no-op refusals, reactivation and persistence failures separately from job recovery.
