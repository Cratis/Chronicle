<!-- cratis-ai-managed: skills/cratis-screenplay-toolchain/references/generated-responses-example.md -->
# Generated values and responses example (ESM v7)

A small model with a generated identifier, a second generated value, an ordered record response, a scalar
response, and specifications that pin both the emitted events and the response. It compiles with 0
diagnostics, binds, and its two specifications pass in the reference runner. Rules and refusals:
[executable-subset.md](executable-subset.md#generated-values-and-responses-esm-v7).

Requires Screenplay 4.68.0 or later (`screenplay --version`); older compilers refuse these constructs with `PLAY0268`.

```screenplay
// ESM v7 reference-executable example: deterministic generated fixtures, events and command responses.
concept ProjectId : Uuid
concept ReceiptId : Uuid
concept ProjectName : String
module Projects
  feature Registration
    slice StateChange Register
      command RegisterProject
        projectId ProjectId generated identifier
        receiptId ReceiptId generated
        name ProjectName
        produces event ProjectRegistered
          name ProjectName = name
        returns
          projectId = projectId
          receiptId ReceiptId = receiptId
      specification RegisteringReturnsIdentifiers
        when RegisterProject
          for "11111111-1111-1111-1111-111111111111"
          generated receiptId = "22222222-2222-2222-2222-222222222222"
          name = "Apollo"
        then ProjectRegistered
          for "11111111-1111-1111-1111-111111111111"
          name = "Apollo"
        then returns
          receiptId = "22222222-2222-2222-2222-222222222222"
    slice StateChange Rename
      command RenameProject
        projectId ProjectId identifier
        name ProjectName
        produces event ProjectRenamed
          name ProjectName = name
        returns projectId
      specification RenamingReturnsIdentifier
        when RenameProject
          projectId = "11111111-1111-1111-1111-111111111111"
          name = "Apollo"
        then ProjectRenamed
          for "11111111-1111-1111-1111-111111111111"
          name = "Apollo"
        then returns "11111111-1111-1111-1111-111111111111"
```

What each part pins:

- `for "<uuid>"` in `when` supplies the generated identifier's fixture; an indented `generated <name> = <value>` supplies any other generated value. Neither is a request input.
- `then returns` with a named field asserts a subset of the record; `then returns "<uuid>"` asserts a scalar response.
- Without a fixture for a reached generated value the run is `Unsupported(IdentityAllocation)` and the specification never passes.
