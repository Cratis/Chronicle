<!-- cratis-ai-managed: skills/cratis-components-schema-editor/references/tree-layout.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# SchemaEditor tree layout

## Contents

- Live edits and controlled mode
- Extension props
- Conversion helpers

Verified at Components v4.26.4: `Source/SchemaEditor/SchemaEditor.tsx`,
`Tree/TreeSchemaEditor.tsx`, `Tree/schemaConversion.ts`, and `index.ts`.
All exports below come from `@cratis/components/SchemaEditor`.

## Live edits and controlled mode

`layout='tree'` shows nested properties inline. With `schema` and `onChange`,
it owns the property tree and reports every edit as a complete schema through
`ChangeHandler<JsonSchema>` (the optional metadata argument identifies a user edit).
Handing the reported schema back preserves property ids; a different external
schema replaces the tree. `readOnly` disables edits, not selection or host slots.
Table-only Edit/Save/Cancel and `typeFormats` do not govern this layout.

Alternatively pass `properties: Property[]`: the host owns all changes. An edit
with no matching callback is not offered. Handle `onAddProperty`,
`onAddChildProperty`, `onDeleteProperty`, `onRenameProperty`,
`onChangePropertyType`, `onSetKeyProperty`, and `onSetRequiredProperty` as needed.
`schema` is ignored in this mode. `onChange`, `onPropertiesChange`,
`onPropertyRemoved`, and `onPropertyRenamed` report editor-owned changes only;
the controlled host applies and reports its own changes.

## Extension props

| Prop | Contract |
| --- | --- |
| `concepts: PropertyConcept[]` | Named types with a `PropertyType`; saved as `x-concept`. Overrides a surrounding `PropertyConceptsProvider` |
| `allowRequired` | Toggle membership in the object's `required` names |
| `allowKeyProperty` | Boolean or `(property, context) => boolean`; `x-key`, one per object |
| `isPropertyProtected` | Prevent rename/removal, not every possible edit |
| `validatePropertyName` | `(name, property, siblings) => string \| undefined`, after built-in checks |
| `selectedPropertyId`, `onPropertyClick` | Controlled selection; callback receives the property id |
| `header`, `footer` | Content above properties / beside the add button |
| `renderPropertyLeading`, `renderPropertyAccessory`, `renderPropertyDetails` | `(property, context) => ReactNode`, before name / after row content / below row |
| `renderPropertyTypeBadge` | `(property, context, defaultBadge) => ReactNode` |
| `getPropertyRowState` | Extra row classes, data attributes, tooltip and type-lock state |
| `pt: SchemaEditorParts` | Stable part attributes |
| `aria-label`, `aria-labelledby`, `labels` | Localizable editor name and copy |

In controlled mode, supplying `onSetKeyProperty` or `onSetRequiredProperty`
enables the corresponding toggle unless explicitly disabled. Both layouts also
support `header`, `footer`, protection, extra name checks and rename/removal callbacks.

## Conversion helpers

The subpath exports `PropertyType`, `PropertyConceptsProvider`,
`jsonSchemaToProperties`, `propertiesToJsonSchema`, `addProperty`,
`addChildProperty`, `renameProperty`, `removeProperty`, `changePropertyType`,
`setKeyProperty`, `setRequiredProperty`, and `toggleRequiredProperty`.
Use the typed property-tree helpers for a controlled host, rather than treating
`required` as a property boolean. Schema keywords are `x-concept`, `x-key`, and
the parent object's `required: string[]`.
