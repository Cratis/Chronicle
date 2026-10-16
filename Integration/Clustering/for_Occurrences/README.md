# Live occurrence recovery specifications

`OccurrenceScenarios` is shared by this MongoDB two-silo collection and `Integration/Kernel/Occurrences`, which selects one PostgreSQL-backed silo. Both use two independent application hosts/connections and the production client delivery loop. `CapableOccurrenceConnection` reads the actual kernel compatibility response; it does not hardcode capabilities or bypass admission.

The scenarios cover:

- one Startup occurrence, receipt and persisted sample application event per logical activation across two hosts;
- leased work surviving a total kernel restart, reclaimed at exactly epoch + 1, with one receipt/event and stale completion refused;
- instance loss after an effect commit but before occurrence completion, recovered by the remaining client with one receipt/event;
- AllNamespaces fan-out paused after a real 100-namespace prefix commit, followed by abrupt silo termination, fresh kernel recovery, and one occurrence per original snapshot namespace.

The test-only service composition also supplies the actual generated Occurrences service missing from the legacy in-process `IServices` composite. It is activated by the real silo's dependency-injection container, not a fake. The test-only storage decorator forwards every operation to the real provider. Its barrier observes a durable prefix; it neither creates a fake successful write nor replaces the kernel's clock. The backing database is retained when TestingHost kills and rebuilds the silos. The SQL fixture verifies its actual system-storage implementation and retains its PostgreSQL container while replacing the silo. The container is tagged `cratis.task=4911-p3c-postgres` for scoped cleanup.

The MongoDB instance-loss case kills the silo hosting the first client, then cancels that test client's remaining in-process worker tasks. The SQL variant tears down the first application connection without stopping the only silo. The latter is **not** an operating-system process-kill test.

## Remaining process boundary

Two `ActivationScope.PerProcess` hosts in the same test runner are one process and correctly share the SDK's static process identity. Proving two Startup occurrences for two processes requires a separate-process application host connected to a current kernel over gRPC. These specifications deliberately do not reset private static fields or invent two process identities. The two-process Startup scenario and SQL application OS-process kill therefore remain separate coverage gaps.

## Released-kernel compatibility (opt-in)

`Integration/Client/for_OldKernelOccurrences` checks that a current client fails closed against a released kernel without the occurrence and effect-receipt capabilities. It is **opt-in**: its facts are reported as skipped unless `CHRONICLE_OLD_KERNEL_SPECS=1` is set, and it needs a `-p:Version=19.74.0` build of the client and its project references (the ordinary CI artifact is not release-stamped). The command is in that directory's `README.md`. Two scenarios remain deferred and are tracked in Cratis/Chronicle#5001.

## Running

Run the two namespaces separately with `.github/scripts/run-tests-with-retry.sh` and the bounded integration phase runner. Check one-minute load is below 60 before admitting either phase. Use `TEST_MAX_ATTEMPTS=1` for diagnosis; change code or gather new evidence before a retry. No sleep-based assertions or Ryuk-disabling environment overrides are needed.
