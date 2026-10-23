<!-- cratis-ai-managed: skills/cratis-chronicle-client-dotnet/references/immediate-append-refusal.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Refuse immediate appends before transport

Requires Chronicle 19.81.0. Check with `dotnet list package`; older clients cannot
prevent an escaped immediate append with this API. Return events or stage them
transactionally instead; detecting an immediate append after it committed is
not a retry-safe boundary.

`IUnitOfWork.RefuseImmediateAppends()` returns a disposable frame. While its first
frame is alive, native immediate append entry points in the current flow throw
before transport, including captured sequence references and references to a
different store. Transactional appends still enroll in the unit. This does not
enable read protection or change read admission.

## Who completes

- With no existing completion owner and no enforced read protection, the first
  frame owns completion. Use that frame's asynchronous `Commit` or `Rollback`.
- Inside an ASP.NET-owned unit, or an enforcing read-protected unit, the frame is
  refusal-only. Its `Commit` and `Rollback` throw
  `ImmediateAppendRefusalRequiresOwner`; completion stays with the existing owner.
- Nested frames borrow the refusal scope; they do not acquire completion rights.
  Refusal lasts until the first frame is disposed, even if it already committed.
- Replacing the ambient unit with `Begin` or `SetCurrent` while the frame lives
  is refused. Dispose the frame at the boundary rather than trying to escape it.

This prevents immediate Chronicle writes, not external I/O. Use it around a
custom unit that must commit atomically with a receipt, and handle the ordinary
commit result. Do not assume Arc's occurrence executor installs this frame.

## Verify

In a spec, attempt an immediate append through both the current event log and a
previously captured reference; assert that neither reaches transport. Assert
that transactional work commits with its receipt, that an unsupported kernel
fails closed, and that borrowed frames cannot complete another owner's unit.
Run `dotnet test` on the owning project. In-process proof is not a substitute for
integration coverage of the storage provider's transaction boundary.
