<!-- cratis-ai-managed: skills/cratis-screenplay-ui-composition/references/scene-runtime-boundaries.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Scene runtime and composition boundaries

Reviewed against Scene v4.82.0; the product inventory owns the review baseline.
These are Scene package/host contracts, **not new Screenplay syntax**. Check Stage's
emission support and pinned Scene version separately; a newer Scene release does
not upgrade a generated application.

## Query input is opt-in, exact and submit-driven

`Cratis.Components:queryInputForm` owns local string drafts and commits arguments
only on submit. Editing does not execute or import the query runtime; it hides a
settled result and cancels pending work. Editing back does not revive an old
result. Explicit resubmission can retry identical values. Disabling, losing a
binding or unmounting discards the session and cancels work; stale responses must
not replace newer state.

The host supplies a registered zero-argument native optional-single Arc proxy,
its Arc context, explicit input declarations and a scalar own result-field name.
This is not a general collection form, platform-neutral Form schema or guessed
route. Query and parameter names are case-sensitive. Inputs are strings, with
explicit required/pattern rules: do not trim, coerce, manufacture identifiers or
infer a GUID/business format. Optional empty strings remain present. Descriptor
compatibility is bounded to scalar `String`/`Guid`; unsupported, missing,
duplicate or enumerable descriptors refuse visibly before execution. A nonempty
malformed GUID needs an authored pattern or backend validation; Scene does not
invent that validation.

Stage hosts should register each source using `registerQueryIdentity`, not first
collapse same-named sources into a name-keyed object. `resolveExactQuery` treats
zero candidates as missing and multiple candidates as ambiguous, including a
legacy plus identity registration. `registerQuery`/`registerQueries` still replace
by name for legacy hot reload: that is **not** collision detection. A live exact
binding collision cancels the form rather than selecting a candidate. Clear both
registries when switching applications.

See the [query input form contract](https://github.com/Cratis/Scene/blob/main/Documentation/components-package/query-input-form.md).
These are host API names, not `.play` declarations.

## Root templates and canonical profiles

The root template is the application chrome, not another screen layout. Its
`primaryOutlet` names the screen content slot (`content` by default). An
application-owned root's authored slot content wins over default host chrome;
blueprint placeholder content can be replaced by host chrome. A root-role screen
template is application-scope only and cannot fit a slot; a dialog template cannot
be a root. Root option names belong to the chosen shell, not universal CSS.

Preserve the profile's root reference/options, appearance, identity, sample-data
selection and default screen-template choice when editing it. Legacy `layout`
remains readable; when both exist, `root` wins. New writers should use `root`.

`UiProfileJson` and `parseUiProfile`/`serializeUiProfile` have a shared canonical
JSON contract: camelCase names, declared enum names, omitted absent members, no
whitespace, declaration ordering for known model members, ordinal ordering for
other object keys, and preserved array order. Readers match names/enums
case-insensitively and ignore unknown members. Do not confuse this lenient reader
with permission to invent accepted profile properties or discard authored known
members. C# and TypeScript parity is checked against the same fixtures.

See the [root-template and serialization contract](https://github.com/Cratis/Scene/blob/main/Documentation/blueprints/root-template.md)
and [profile designer contract](https://github.com/Cratis/Scene/blob/main/Documentation/blueprints/ui-profile-designer-contract.md).

## Interaction support remains host-specific

Navigation/dialog execution needs the host's dispatcher/history wiring and the
same application/binding providers as the screen. Serializing an action is not
proof it runs. The generic `runActions` runner stops on a declined confirmation,
reports unsupported actions, and ends the old screen's action sequence after
navigation. Its command completion branch still does not install an action-local
`response` scope. Keep authored response continuations, but report missing host
binding support instead of rewriting them to unrelated state; see
[response continuation admission](response-continuations.md).

See the [action runner](https://github.com/Cratis/Scene/blob/main/Source/JavaScript/engine/runActions.ts).

## Component text and runtime packaging

Scene's component cross-cutting rules use a strings table merged with the
element's `strings`, and `Intl` formatting for the element's `locale`. These are
component contracts, not new `.play` properties or an application-wide locale
service. Preserve logical-direction styling, density and permission behavior
when supplying localized component text.

`@cratis/scene.components/runtime` remains the runtime-only entry; design-time
editors are not required to execute a screen. Do not add design-time dependencies
to work around a missing runtime registration.

See the Scene component package documentation for the registered component's
strings/locale support and the runtime entry's supported registrations.
