---
applyTo: "**/*.ts, **/*.tsx"
---

# TypeScript Conventions

TypeScript's type system is the primary tool for catching bugs before they reach production. Every rule here pushes toward maximum compiler coverage and self-documenting code. If the types are right, the code almost writes itself.

## Variables and Naming

- Prefer `const` over `let` over `var`.
- Never use shortened or abbreviated names for variables, parameters, or properties.
  - Use full descriptive names: `deltaX` not `dx`, `index` not `idx`, `event` not `e`, `previous` not `prev`, `direction` not `dir`, `position` not `pos`, `contextMenu` not `ctx`/`ctxMenu`.
  - The only acceptable short names are well-established domain terms (e.g. `id`, `url`, `min`, `max`).
- Never leave unused import statements in the code.
- Always ensure the code compiles without warnings — run `yarn compile` to verify (no output means success).

## Folder Structure and Naming

- Do not prefix a file, component, type, or symbol with the name of its containing folder or concept — use folder structure to provide that context instead.
- Favor functional folder structure over technical folder structure.
  - Group files by the feature or concept they belong to, not by their technical role.
  - Avoid folders like `components/`, `hooks/`, `utils/`, `types/` at the feature level.

## Enums over Magic Strings

String literal unions look concise but provide no refactoring support, no namespace, and no discoverability. Enums give you all three — plus `switch` exhaustiveness checking.

```ts
// ✅ Correct — refactorable, discoverable, exhaustive
export enum SliceType {
    StateChange = 'stateChange',
    StateView   = 'stateView',
    Automation  = 'automation',
    Translator  = 'translator',
}

// ❌ Wrong — no refactoring support, invisible to tooling
export type SliceType = 'stateChange' | 'stateView' | 'automation' | 'translator';
```

- Use enum members everywhere — `switch` cases, comparisons, defaults.
- Do **not** import enums as `type`; they are values.
- Export enums from `index.ts` without the `type` keyword.

## One Type or Enum per File

Each type gets its own file because it makes the codebase navigable — finding `SliceType` means opening `SliceType.ts`, not hunting through `types.ts`. It also keeps diffs clean and makes imports explicit.

- Every interface, type alias, and enum lives in **its own file**, named after the type (e.g. `SliceType.ts`).
- **Never create** `types.ts`, `models.ts`, `interfaces.ts` grab-bag files — they become dumping grounds that grow without limit.
- Exception: component props interfaces (`*Props`) may live alongside their component `.tsx` file since they are tightly coupled to that component.
- Aggregate exports through the folder's `index.ts`.

## Type Safety

`any` disables the compiler — the one tool that catches bugs for free. Every `any` is a hole in the safety net. Use `unknown` and narrow with type guards instead.

- Never use `any` — use `unknown`, `Record<string, unknown>`, or proper generic constraints.
- Use proper generic constraints: `<TCommand extends object = object>` not `= any`.
- Use `unknown` as the default generic parameter: `<T = unknown>` not `<T = any>`.
- Prefer `value as unknown as TargetType` over `value as any`.
- For objects with dynamic properties: `(obj as unknown as { prop: Type }).prop`.

### Event Handlers

- React synthetic events (`React.MouseEvent<Element, MouseEvent>`) and DOM events (`MouseEvent`) are different types — don't mix them.
- Convert between them: `nativeEvent as unknown as React.MouseEvent`.
- Use `e.preventDefault?.()` instead of `(e as any).preventDefault?.()`.

### Generic React Components

- Use `React.ComponentType<Props>` for React component types.
- For Storybook components with no props: `React.ComponentType<Record<string, never>>`.
- When converting component imports in Storybook: always `as unknown as` to avoid type mismatch errors.
- Properly type story args — never use `any`.

### External Libraries

- Use proper library types when available (e.g. PIXI types, not `any`).
- Use specific property types: `{ canvas?: HTMLCanvasElement }` instead of `any`.
- When library generics have strict constraints, thread types through `unknown`: `props.command as unknown as Constructor<Command<...>>`.
- Extract tuple results explicitly rather than destructuring when type assertions are needed.

### Unknown Values

- Add type guards before using function parameters of unknown type: `if (typeof accessor !== 'function') return ''`.
- Type parameters with fallbacks: `function<T = unknown>(accessor: ((obj: T) => unknown) | unknown)`.
- Cast unknown objects to record before property access: `(obj as Record<string, unknown>).items`.
- Use `String(value)` for string conversions; `new Date(value as string | number | Date)` for dates.

## Localised Strings

All user-visible text **must** come from translation files — never hard-code UI strings directly in component code. This is not optional even for English-only deployments — it centralizes copy, enables future localization, and makes it possible to audit all user-facing text in one place.

### Structure

```
Source/Core/
  Strings.ts                    ← re-exports the default (English) JSON
  Locales/
    en/
      translation.json          ← all English strings, organized by feature/component
```

`translation.json` is a nested JSON object whose top-level keys group strings by feature or component (e.g. `projects`, `eventModeling`, `chat`). Add new keys under the appropriate existing group, or add a new top-level group if the feature is new.

### Importing

Import the strings object using the `Strings` path alias (configured in `tsconfig.json`):

```ts
import strings from 'Strings';
```

Do **not** use relative paths such as `'../../Strings'` or `'../Strings'`.

### Usage

Access strings directly through the nested object — TypeScript infers the full type from the JSON file:

```tsx
import strings from 'Strings';

export const MyComponent = () => (
    <Button label={strings.projects.addProject} />
);
```

For attribute strings (e.g. `title`, `placeholder`, `aria-label`):

```tsx
<div title={strings.eventModeling.grid.detailPanel.event}>
    ...
</div>
```

### Adding New Strings

1. Add the key to `Source/Core/Locales/en/translation.json` under the appropriate group.
2. TypeScript picks up the new key automatically from `Strings.ts` (no regeneration step).
3. Use the key via `strings.<group>.<key>` in the component.

### Rules

- **Never** use plain string literals for user-visible text in JSX or attribute props. This includes `label`, `header`, `placeholder`, `title`, `aria-label`, `emptyMessage`, and any visible text nodes.
- Only constant, non-localised values are allowed as raw strings (CSS class names, `key` props, internal identifiers).

## File Header

Every TypeScript file must start with:

```typescript
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
```

## Generated Files

**Never edit generated files.** Files produced by the proxy generator (`dotnet build`), code scaffolding tools, or any other automated tool must not be modified by hand — in any language. Generated files are overwritten on the next build, so hand-edits are silently lost and create false confidence that a fix is in place.

- If the generated output is wrong, fix the **source** (the C# record, the template, or the generator configuration) and rebuild.
- Generated TypeScript proxy files (commands, queries, observable queries) are always regenerated from C# sources. Never edit them directly.

## Arc Frontend Patterns

Arc's proxy generator bridges C# and TypeScript automatically — every `[Command]` and `[ReadModel]` becomes a TypeScript class with `.use()` hooks, `.execute()` methods, and change tracking. This is the foundation of full-stack type safety: change a C# record and the TypeScript proxy updates on the next `dotnet build`.

### Commands

Auto-generated from C# `[Command]` records. The `.use()` hook returns a tuple: the command instance (with change tracking) and a setter for property values.

```tsx
const [command, setValues] = OpenAccount.use({ name: '', owner: '' });
await command.execute();       // Sends command to backend, returns CommandResult
await command.validate();      // Pre-flight validation only, no side effects
command.hasChanges;            // True when any property differs from initial values
command.revertChanges();       // Reset all properties to initial values
```

### Queries

Auto-generated from C# `[ReadModel]` static query methods. Observable queries (returning `ISubject<T>` on the backend) auto-subscribe via WebSocket — the component re-renders when data changes on the server.

```tsx
const [result, perform] = AllProjects.use();
// result.data — the query result
// result.isPerforming — true while loading
// result.hasData — true when data has arrived
// result.isSuccess — true when query completed without errors
```

Paginated queries:
```tsx
const [result, , setPage] = AllProjects.useWithPaging(10);
```

### CommandScope

Wraps multiple command-using components, aggregating their `hasChanges` state and enabling bulk `execute()` and `revertChanges()`. Useful for forms that span multiple components.

### CommandForm

Declarative form component with built-in field types, validation timing (`validateOn: blur|change|both`), and automatic server-side validation feedback.
