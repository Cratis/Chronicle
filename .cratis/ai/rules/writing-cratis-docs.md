---
applyTo: "**/Documentation/**/*.{md,mdx}"
paths:
  - "**/Documentation/**/*.md"
  - "**/Documentation/**/*.mdx"
---
<!-- cratis-ai-managed: rules/writing-cratis-docs.md -->

# Writing Cratis Documentation

Read and use **cratis-documentation-writing** to establish the reader's goal and
page purpose, then **cratis-engineering-docs-authoring** to draft from product
evidence. Before writing explanations beside examples, work through
[Explaining code](../skills/cratis-engineering-docs-authoring/references/explaining-code.md).
It shows how a concrete registration or directive can carry the explanation
without making the page read like a review form. Read a current, well-reviewed
tutorial in the product or a closely related product before writing, without
assuming its vocabulary fits this product.

Write as a developer explaining the code to another developer. Start with the
reader's problem, show the relevant example, and explain the part they cannot
see: why this type matters, what the call changes, or where responsibility
passes to another component. Let each paragraph develop that point. Use steps
for a procedure and a table for a comparison; neither replaces a connected
explanation. A reference entry can state its contract directly.

The example should also teach the intended idiom. Explain a tempting alternative
when it exposes a real trap, and put a limitation beside the action it affects.
Do not add a stock warning or a closing list of everything the product does not
do. A concrete recommendation with a source-backed reason is useful perspective;
an invented customer story or personal experience is not. Make the first example
the convention path: do not hand-register what the framework already discovers.
Explain that invisible wiring against the backend the reader actually built,
then show an observable result before introducing another concept.

Use **cratis-technical-examples** to verify code, configuration, commands and
results against the applicable product source. It is fine to explain the effect
of code that is absent, such as convention-based wiring, but establish that
effect first. Do not infer a second client's API or behavior from the first.
Read the multi-client section of **Explaining code** before choosing shared
prose, tabs or separate guides. Preserve the owning repository's snippet and
language-tab workflow rather than inventing another one. Show both the backend
contract and generated frontend shape when both matter, keeping required
producer/consumer actions sequential.

Choose presentation to support the reader, not decorate the page. The exact
Markdown/MDX boundary, aside semantics, component contracts and rendering checks
live in [Documentation Structure and Formatting](documentation-structure-and-formatting.md).
Use the owning site's supported components and inspect the emitted Markdown;
a raw source mirror is not proof that essential labels or warnings survive.

Link the guided explanation to exact reference and connect neighboring products
at their workflow seams instead of retelling their setup. A coming-from-X bridge
can put the reader's existing code beside the Cratis equivalent and explain
what changes. Keep that comparison fair and version-accurate, or label it
illustrative; say when the existing approach remains the better fit. It does
not impose universal navigation buckets.

Follow the [documentation rule](documentation.md) for naming, claims and version
constraints. The reader needs the minimum or compatibility boundary that changes
what they can run, not the author's source-checking diary. Keep verification
provenance in the private change summary. A rendered page, compiling example,
observed result and successful reader journey are different evidence.

Before returning a draft, read its explanation as prose: does it make the code
easier to understand, or merely name responsibilities and limitations? Use
**cratis-writing-voice-and-cadence** for an authorized revision, preserving
technical claims and approval boundaries. For a materially changed journey,
follow the reader-review procedure owned by **cratis-documentation-writing**;
report when independent review is unavailable. Do not edit generated copies,
change navigation, or publish as part of writing. Run the owning documentation
gate and, when available, the Documentation site's full check for attributable
lint and rendered-link errors. For a visual page, use the owning preview workflow
and inspect light and dark screenshots. Source ownership and the edit/sync/verify
loop live in [Editing Cratis Documentation](editing-cratis-docs.md).
