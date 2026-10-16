<!-- cratis-ai-managed: skills/cratis-chronicle-reducer/references/replay-hooks-and-definition-changes.md -->
# Replay hooks and definition changes

This reference holds the optional self replay hooks and how definition changes classify into replay.

## Optional self replay hooks

Independently optional public static non-generic hooks return exactly `Task`:

```csharp
public static Task BeginReplay() => Task.CompletedTask;
public static Task EndReplay() => Task.CompletedTask;
public static Task BeginReplayPartition(Partition partition) => Task.CompletedTask;
public static Task EndReplayPartition(Partition partition) => Task.CompletedTask;
```

Place these methods on the model and import `Cratis.Chronicle.Observation` for
`Partition`. Global hooks accept only trailing class/interface service arguments;
partition hooks require `Partition` first. Services resolve in the notification's
async DI scope; missing registrations fail. Events, the model, `EventContext`
and extra partition arguments are not hook services. Chronicle awaits hooks and
scope disposal; absent hooks create no scope. Do not assume full replay delivers
partition notifications, failures deliver an end hook, or delivery is exactly
once. Instance reducers retain replay notification interfaces.

## Definition changes replay

The kernel classifies new definitions under `observers.definitionEvolution`:
`Automatic` (default), `PartialOnly`, or `Manual`. Reducer method fingerprints
make code changes visible even without subscription changes; assembly versions
are ignored. Changed
implementation, keys or filters require full replay; adding event types permits
partial replay only when history proves they follow all previously consumed
events per source. Interleaved history requires full replay. Manual policies
leave recommendations; they do not silently rebuild. Source:
`Documentation/hosting/configuration/observers.md`.
