<!-- cratis-ai-managed: skills/cratis-screenplay-slice-design/references/screens-before-slices.md -->
# Screens before slices

Syntax, severities and diagnostics of free screens, shapes, `uses screen` and provisions are owned by `cratis-screenplay-ui-composition` ([free screens](../../cratis-screenplay-ui-composition/references/free-screens.md)). This page covers the design decisions: when to draw the screen first, what the contract is, and how the slice that serves it stays honest.

## Where the screen lives

| Situation | Declare the screen |
| --- | --- |
| Exactly one slice serves it and nothing else needs it | In that slice (unchanged). |
| It is drawn before any slice exists, or a storyline walks through it | Free: top level, module or feature, with its data as needs. |
| Two or more slices render the same screen | Free, in the module or feature they share; each slice adopts it. |
| It belongs to one module or feature but is served by several slices | Free, in that module or feature. |

Never invent a slice only to host a screen, and never copy a screen into several slices to avoid adoption.

## Design order

1. **Level 1 first.** Decide what the screen shows and what the user can do (`data`, `action`), as for any screen. A screen with neither a need nor an action is still a gap.
2. **Name the need as a shape, not as a read model.** The need says what the screen reads (`number`, `amount`, `dueDate`), in the business's words. A read model is chosen later, by the slice, so naming one now defeats the point.
3. **Decide inferred or explicit.** Leave the shape inferred while the screen is a sketch. Declare a `shape` with typed fields when more than one slice will adopt the screen, when sample data must be checked against the contract, or when a reviewer needs the contract written down. An explicit shape is a promise: adopters must meet its types.
4. **One need per purpose.** A screen that shows open and overdue invoices needs two lists. Use aliases (`data Invoice[] as overdue`) and two queries, never one read model with a flag that the screen filters.
5. **Adopt from the slice that owns the view.** The adopting slice is a State View slice whose read model and query already answer the need, or one you add for it. Each need has its own purpose-built read model: specialization over reuse.
6. **Provide, then map.** Provide each need from a query of the right cardinality. Add `from` only where names differ; if many names differ, the read model's names are probably wrong, not the shape's.

## Lineage: a shape field is not an origin

Every field a screen reads still needs an origin ([field lineage](field-lineage.md)): a shape field resolves to a read model property, which resolves to the event property or state that fills it. The compiler checks the first hop (`PLAY1160`, `PLAY1161`); the rest is the lineage pass.

```text
PLAY1160: Read model 'InvoiceRow' of query 'ListInvoices' has no 'dueDate' to provide 'dueDate' of 'Invoice'
```

When this appears, add the property to the slice's read model and trace it to an event the slice already consumes, or add the fact that carries it. Never delete the field from the screen or the shape to make the diagnostic disappear, and never give the screen a default for a value no event knows.

## Reporting

For each free screen, say in two or three sentences what it shows and what the user can do, and one of: **adopted by** a named slice (with the provided needs), or **design-only**, with the slice that should adopt it. A design-only screen is design intent: it is not in the UI contract, and a flow that depends on it is not delivered. A finished model has no unprovided need: `screenplay <model> --warnaserror` must report none.
