---
name: cratis-components-schema-editor
description: Use the Cratis Components schema editors — SchemaEditor for editing a JSON schema's property structure, and ObjectContentEditor for viewing and editing an object instance against a schema. Covers the exact props, the narrow JSON-schema subset they consume, how edits come back out, the type-format vocabulary, property-name validation, and the real editing limits. Use when building a UI that lets a user shape an event or payload schema, or inspect and edit a stored object. Do not use for a command form bound to a generated Arc command.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-components-schema-editor/SKILL.md -->

# Cratis Components schema editors

Two components edit schema-shaped data. They are **not** interchangeable:

| Component | Edits | Subpath |
| --- | --- | --- |
| `SchemaEditor` | the **schema** — property names, types, and formats | `@cratis/components/SchemaEditor` |
| `ObjectContentEditor` | an **object instance**, interpreted through a schema | `@cratis/components/ObjectContentEditor` |

For a form bound to a generated Arc command, use `CommandForm` and its fields
instead — see the **cratis-arc-react-page** skill. These editors are for the
case where the shape itself is data.

## Verified product sources

| Package | Version | Verified from |
| --- | --- | --- |
| `@cratis/components` | `3.0.0` | its package manifest and the `SchemaEditor` / `ObjectContentEditor` sources |
| `primereact` | `^11.0.0` | peer of `@cratis/components@3.0.0` |
| `react` | `^19.0.0` | peer of `@cratis/components@3.0.0` |

## The schema type these editors consume

This is a deliberately narrow, hand-rolled subset — **not** a JSON Schema draft
type. Import it from either editor's subpath.

```ts
interface JsonSchema {
    title?: string; name?: string; $id?: string; $ref?: string;
    type?: string; format?: string; description?: string;
    properties?: Record<string, JsonSchemaProperty>;
    items?: JsonSchema;
    required?: string[];
    definitions?: Record<string, JsonSchema>;
}

interface JsonSchemaProperty {
    id?: string; name?: string; type?: string; format?: string; description?: string;
    items?: JsonSchema; properties?: Record<string, JsonSchemaProperty>;
    required?: boolean; $ref?: string;
}

type Json = string | number | boolean | null | Json[] | { [key: string]: Json };
```

Three limits to design around:

- **`$ref` and `definitions` are declared but never resolved.** Nested
  navigation walks `properties` and `items` literally. A schema that relies on
  references will not render its referenced shapes.
- **`type` is a plain string**, not a union — there is no exhaustiveness check.
- **`required` is read but never rendered or edited** by `SchemaEditor`. Its
  table has two columns, Property and Type. Do not promise a required toggle.

## `SchemaEditor`

```tsx
import { SchemaEditor } from '@cratis/components/SchemaEditor';
import type { JsonSchema, TypeFormat } from '@cratis/components/SchemaEditor';
```

The editor is internally stateful and pushes the **whole new schema** out on
every structural change.

```tsx
const [schema, setSchema] = useState<JsonSchema>(initialSchema);

<SchemaEditor
    schema={schema}
    eventTypeName='User'
    canEdit
    onChange={setSchema}
    onSave={() => persist(schema)}
    onCancel={() => reload()} />
```

### Props

| Prop | Type | Default |
| --- | --- | --- |
| `schema` | `JsonSchema` | **required** |
| `eventTypeName` | `string` | `''` — the breadcrumb root label |
| `canEdit` | `boolean` | `true` |
| `canNotEditReason` | `string` | shown when `canEdit` is false |
| `onChange` | `(schema: JsonSchema) => void` | receives the complete new schema |
| `onSave` | `() => void` | takes **no arguments** |
| `onCancel` | `() => void` | |
| `editMode` | `boolean` | initial edit state only — see below |
| `saveDisabled` | `boolean` | `false` |
| `cancelDisabled` | `boolean` | `false` |
| `typeFormats` | `TypeFormat[]` | the default vocabulary |
| `className` | `string` | |
| `labels` | `Partial<SchemaEditorLabels>` | override every visible and accessible string |

### Two behaviors that decide how you wire it

- **`editMode` is initial state, not a controlled prop.** It seeds the internal
  flag once; changing it later has no effect. Drive edit mode from inside the
  editor, or remount it with a `key` when you must force a mode.
- **`schema` *is* re-synced.** A new `schema` identity resets both the working
  copy and the cancel baseline. Cancel restores that baseline and calls
  `onChange` with the restored clone, so a controlled parent stays consistent.

`onSave` receives nothing — accumulate state from `onChange` and persist what
you already hold. `onSave` also returns the editor to read mode.

### Type and format vocabulary

The type dropdown is built from `typeFormats`, a flat list of
`{ jsonType, format }` pairs; the option label is the format when there is one,
otherwise the JSON type. The container types `array` and `object` are always
appended.

The default vocabulary covers `string` (plain, `guid`, `date-time`, `date`,
`time`), `integer` (plain, `int16`, `int32`, `int64`), `number` (plain, `float`,
`double`), and `boolean`. Supply your own `typeFormats` to narrow or extend it.

**`typeFormats` is the only extension point.** There is no registry mapping a
schema type to a custom editor component — the cell renderers are a fixed
branch on `array` / `object` / everything else. You can change the vocabulary,
not the widget.

Changing a property's type has side effects worth knowing: switching to `array`
seeds `items` as a string and clears `format`; switching to `object` seeds an
empty `properties` and clears `format` and `items`; switching to anything else
clears both `items` and `properties`.

### Nested navigation

Drilling into an object property or an array's item definition pushes a
breadcrumb segment. Array item definitions use the reserved path segment
`$items`, rendered in the breadcrumb as `[items]`. Nested navigation is
**click-driven only** — the breadcrumbs and rows are not keyboard-operable, so
do not document a keyboard path that does not exist.

### Property-name validation

A property name must be non-empty, must match an identifier pattern (a letter
or underscore followed by letters, digits, or underscores), and must be unique
among its siblings. Save is disabled while any name is invalid.

### Localization

`SchemaEditorLabels` has ten fields, five of them accessible names:
`edit`, `save`, `cancel`, `addProperty`, `actions`, `navigateBack`,
`emptyMessage`, `navigateToItemDefinition`, `navigateToProperties`,
`deleteProperty`. Pass `labels` with the ones you want to change — they merge
over the English defaults. Never rely on the built-in English strings in a
localized application.

## `ObjectContentEditor`

```tsx
import { ObjectContentEditor } from '@cratis/components/ObjectContentEditor';
```

Renders an object's values against a schema, read-only by default.

```tsx
const [value, setValue] = useState<Json>(storedObject);
const [hasErrors, setHasErrors] = useState(false);

<ObjectContentEditor
    object={value}
    schema={schema}
    timestamp={occurred}
    editMode
    onChange={setValue}
    onValidationChange={setHasErrors} />
```

### Props

| Prop | Type | Default |
| --- | --- | --- |
| `object` | `Json` | **required** |
| `schema` | `JsonSchema` | **required** |
| `timestamp` | `Date` | optional, displayed with the content |
| `editMode` | `boolean` | `false` |
| `onChange` | `(object: Json) => void` | receives the whole updated object |
| `onValidationChange` | `(hasErrors: boolean) => void` | fires only in edit mode |
| `className` | `string` | |

### Editing limits — design around these

- **Only root-level properties are editable.** Array and object values render
  "not yet supported" text in edit mode, and any nested navigation view is
  read-only by construction.
- **In edit mode every field is treated as required**, regardless of the
  schema's `required` list. The schema's `required` is consulted only in read
  mode. If your object legitimately has optional values, either stay read-only
  or gate saving on your own rules rather than on `onValidationChange`.
- `onValidationChange` never fires outside edit mode, so a read-only view will
  never report errors.

### Field rendering

The editor branches on the property's `type` and `format`: booleans render a
checkbox, numbers an integer-friendly numeric input, `string` with `date-time`
or `date` a date picker (emitting an ISO string, date-only for `date`), a long
string a multi-line text area, and everything else a text input.

Built-in format validation covers `email` and `uri`, plus numeric parseability.
Anything richer belongs in the caller.

## Breadcrumb

`ObjectNavigationalBar` (`@cratis/components/ObjectNavigationalBar`) is the
shared breadcrumb both editors use. Standalone, it takes `navigationPath`,
`onNavigate`, and `backLabel` (which is both the tooltip and the accessible
name — localize it). It is click-only, like the editors' own breadcrumbs.

## Styling

Both editors expose `className` only. Restyle their internals through a global
PrimeReact pass-through preset on the provider rather than per instance — see
the **cratis-components-styling** skill.

## Verify

- Imports use the component subpath, not the root barrel.
- The schema passed in is the narrow subset — no `$ref` or `definitions` is
  relied on for rendering.
- The parent holds the schema or object state and updates it from `onChange`;
  `onSave` is treated as a signal, not a carrier of data.
- `editMode` on `SchemaEditor` is treated as initial state, not a controlled
  prop.
- Nothing in the UI or the documentation promises a required-flag toggle,
  nested object editing, or keyboard navigation in these editors.
- Every visible and accessible string is supplied through `labels` /
  `backLabel` in a localized application.
- Lint and the TypeScript build pass.
