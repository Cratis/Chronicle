---
name: cratis-components-toolbar
description: Build a canvas-style icon toolbar with the Cratis Components Toolbar family — Toolbar, ToolbarButton, ToolbarSeparator, ToolbarGroup, ToolbarSection, ToolbarContext, ToolbarFanOutItem, ToolbarFolder, and the slot components that let a distant part of the tree contribute buttons. Use when building a drawing or diagram tool palette, a zoom or mode control strip, or any icon-button group with active state, context switching, or fan-out sub-panels. Do not use for a page action menu over a data table.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-components-toolbar/SKILL.md -->

# Cratis Components toolbars

The `Toolbar` family builds **canvas-style tool palettes** — the pill-shaped bar
of icon buttons you find in a drawing or diagram editor. For a page action menu
over a list, use `DataPage.MenuItems` instead (see the
**cratis-arc-react-page** skill).

## Verified product sources

| Package | Version | Verified from |
| --- | --- | --- |
| `@cratis/components` | `3.0.0` | its package manifest and `Toolbar` component sources |
| `primeicons` | `^8.0.0` | peer of `@cratis/components@3.0.0` |
| `react` | `^19.0.0` | peer of `@cratis/components@3.0.0` |

## Import from the subpath

Every toolbar export is a flat sibling — there are **no compound members**. It
is `<ToolbarGroup>`, never `<Toolbar.Group>`.

```tsx
import {
    Toolbar, ToolbarButton, ToolbarSeparator, ToolbarGroup,
    ToolbarSection, ToolbarContext, ToolbarFanOutItem, ToolbarFolder,
    ToolbarSlot, ToolbarSlotProvider, ToolbarLayout, useToolbarSlot,
} from '@cratis/components/Toolbar';
```

## The pieces

| Component | Purpose |
| --- | --- |
| `Toolbar` | the pill-shaped container |
| `ToolbarButton` | icon or text button with a tooltip and optional active state |
| `ToolbarSeparator` | divider between groups |
| `ToolbarGroup` | a sub-group inside a toolbar, optionally filled from a slot |
| `ToolbarSection` | animated section that morphs between named contexts |
| `ToolbarContext` | a named set of buttons inside a `ToolbarSection` |
| `ToolbarFanOutItem` | button that slides out a sub-panel of buttons |
| `ToolbarFolder` | button that opens a grid or list of buttons |
| `ToolbarSlotProvider` / `ToolbarSlot` / `useToolbarSlot` | contribute buttons from elsewhere in the tree |
| `ToolbarLayout` | a named region rendered from slot content |

## Step 1 — A basic toolbar

Buttons default to a vertical layout. **`title` is the required prop and it is
what appears in the tooltip** — there is no `tooltip` prop on `ToolbarButton`.

```tsx
import { Toolbar, ToolbarButton } from '@cratis/components/Toolbar';

export const DrawingToolbar = () => (
    <Toolbar>
        <ToolbarButton icon='pi pi-arrow-up-left' title='Select' />
        <ToolbarButton icon='pi pi-pencil' title='Draw' />
        <ToolbarButton icon='pi pi-stop' title='Rectangle' />
    </Toolbar>
);
```

`icon` accepts either a PrimeIcons class string or a React node, so a custom SVG
component works directly: `<ToolbarButton icon={<CircleIcon />} title='Circle' />`.

## Step 2 — Active state

```tsx
const [activeTool, setActiveTool] = useState('select');

<Toolbar>
    <ToolbarButton icon='pi pi-pencil' title='Draw'
        active={activeTool === 'draw'} onClick={() => setActiveTool('draw')} />
    <ToolbarButton icon='pi pi-stop' title='Rectangle'
        active={activeTool === 'rect'} onClick={() => setActiveTool('rect')} />
</Toolbar>
```

## Step 3 — Separators and horizontal layout

Give `ToolbarSeparator` the same `orientation` as its `Toolbar`, and move
tooltips out of the bar's way on a horizontal toolbar:

```tsx
<Toolbar orientation='horizontal'>
    <ToolbarButton icon='pi pi-minus' title='Zoom out' tooltipPosition='bottom' onClick={() => setZoom(z => z - 10)} />
    <ToolbarButton text={`${zoom}%`} title='Reset zoom' tooltipPosition='bottom' onClick={() => setZoom(100)} />
    <ToolbarButton icon='pi pi-plus' title='Zoom in' tooltipPosition='bottom' onClick={() => setZoom(z => z + 10)} />
    <ToolbarSeparator orientation='horizontal' />
    <ToolbarButton icon='pi pi-question-circle' title='Help' tooltipPosition='bottom' />
</Toolbar>
```

Use `text` for a button that displays a value, such as a zoom percentage.

## Step 4 — Animated context switching

`ToolbarSection` shows one `ToolbarContext` at a time. When `activeContext`
changes the buttons fade out, the section morphs to the new size, and the new
buttons fade in. Only the section transitions; buttons outside it are
unaffected.

```tsx
const [mode, setMode] = useState<'drawing' | 'text'>('drawing');

<Toolbar>
    <ToolbarButton icon='pi pi-arrow-up-left' title='Select' />
    <ToolbarSection activeContext={mode}>
        <ToolbarContext name='drawing'>
            <ToolbarButton icon='pi pi-pencil' title='Draw' />
            <ToolbarButton icon='pi pi-stop' title='Rectangle' />
        </ToolbarContext>
        <ToolbarContext name='text'>
            <ToolbarButton icon='pi pi-align-left' title='Align left' />
            <ToolbarButton icon='pi pi-align-center' title='Align center' />
        </ToolbarContext>
    </ToolbarSection>
    <ToolbarButton icon='pi pi-undo' title='Undo' />
</Toolbar>
```

## Step 5 — Fan-out and folders

`ToolbarFanOutItem` slides a strip of extra buttons out of its trigger. It is
the one component whose label prop **is** called `tooltip`, and both `icon` and
`tooltip` are required.

```tsx
<ToolbarFanOutItem icon='pi pi-th-large' tooltip='Shapes' fanOutDirection='left'>
    <ToolbarButton icon='pi pi-stop' title='Rectangle' />
    <ToolbarButton icon='pi pi-circle' title='Circle' />
</ToolbarFanOutItem>
```

`fanOutDirection` is `'right'` (default), `'left'`, `'up'`, or `'down'` — fan
away from the screen edge the toolbar sits against.

`ToolbarFolder` opens a larger panel of buttons as a grid or a list:

```tsx
<ToolbarFolder icon='pi pi-th-large' title='Tools' mode='list'>
    <ToolbarButton icon='pi pi-pencil' title='Draw' />
    <ToolbarButton icon='pi pi-eraser' title='Erase' />
</ToolbarFolder>
```

`mode` is `'grid'` (default) or `'list'`; `maxColumns` (default `5`) applies to
grid mode only, and `folderDirection` is `'right'` or `'left'`.

## Step 6 — Contributing buttons from elsewhere

A slot lets a component far from the toolbar contribute buttons without prop
drilling. Wrap the region in `ToolbarSlotProvider`, publish content with
`ToolbarSlot`, and consume it either by giving a `ToolbarGroup` a `slotName` or
by rendering a named `ToolbarLayout`.

```tsx
<ToolbarSlotProvider>
    {mode === 'draw' && <ToolbarSlot slotName='tool-options' order={10}>{drawTools}</ToolbarSlot>}
    <Toolbar>
        <ToolbarGroup>
            <ToolbarButton icon='pi pi-arrow-up-left' title='Select' />
        </ToolbarGroup>
        <ToolbarGroup slotName='tool-options' />
    </Toolbar>
</ToolbarSlotProvider>
```

`ToolbarSlot` renders nothing where it sits; its children appear in the matching
consumer, ordered ascending by `order` (default `0`). `useToolbarSlot(slotName)`
returns the current content for a custom consumer. `ToolbarContext` also accepts
a `slotName`.

## Props

### `Toolbar`

| Prop | Type | Default |
| --- | --- | --- |
| `children` | `ReactNode` | required |
| `orientation` | `'vertical' \| 'horizontal'` | `'vertical'` |
| `draggable` | `boolean` | `false` — makes child buttons draggable |
| `onItemDragStart` | `(data: unknown, event: React.DragEvent) => void` | — |

### `ToolbarButton`

| Prop | Type | Default |
| --- | --- | --- |
| `title` | `string` | **required** — the accessible name and the tooltip text |
| `icon` | `string \| ReactNode` | — |
| `text` | `string` | — |
| `active` | `boolean` | `false` |
| `onClick` | `() => void` | — |
| `tooltipPosition` | `'top' \| 'right' \| 'bottom' \| 'left'` | `'right'` |
| `draggable` | `boolean` | inherited from the enclosing `Toolbar` |
| `data` | `unknown` | payload handed to the drag handler |
| `onDragStart` | `(data: unknown, event: React.DragEvent<HTMLButtonElement>) => void` | — |

### `ToolbarSeparator`

`orientation`: `'vertical' \| 'horizontal'`, default `'vertical'`.

### `ToolbarGroup`

`children?: ReactNode` · `slotName?: string` · `orientation?: 'vertical' | 'horizontal'` (default `'vertical'`).

### `ToolbarSection`

`activeContext?: string` · `children: ReactNode` · `orientation?: 'vertical' | 'horizontal'` (default `'vertical'`).

### `ToolbarContext`

`name: string` (**required**) · `children: ReactNode` · `slotName?: string`.
Renders nothing itself — it is a marker `ToolbarSection` reads.

### `ToolbarFanOutItem`

`icon: string | ReactNode` (**required**) · `tooltip: string` (**required**) ·
`tooltipPosition?` (default `'right'`) ·
`fanOutDirection?: 'right' | 'left' | 'up' | 'down'` (default `'right'`) ·
`children: ReactNode`.

### `ToolbarFolder`

`icon: string | ReactNode` (**required**) · `title: string` (**required**) ·
`tooltipPosition?` (default `'right'`) ·
`folderDirection?: 'right' | 'left'` (default `'right'`) ·
`mode?: 'grid' | 'list'` (default `'grid'`) · `maxColumns?: number` (default `5`) ·
`children: ReactNode`.

### `ToolbarSlot` / `ToolbarSlotProvider` / `ToolbarLayout`

`ToolbarSlot`: `slotName: string` · `order?: number` (default `0`) · `children: ReactNode`.
`ToolbarSlotProvider`: `children: ReactNode`.
`ToolbarLayout`: `name: string` · `children?: ReactNode` (the fallback when the
slot is empty) · `orientation?: 'vertical' | 'horizontal'` (default `'vertical'`).

## Multiple groups

Render separate `Toolbar` instances for visually distinct bars, or use
`ToolbarGroup` inside one bar when the groups belong together.

## Verify

- Every import comes from `@cratis/components/Toolbar`, not the root barrel.
- Every `ToolbarButton` has a `title` — it is required and it is the accessible
  name, not decoration.
- `ToolbarSeparator` orientation matches its `Toolbar`.
- Horizontal toolbars set `tooltipPosition` to `'top'` or `'bottom'`.
- Each `ToolbarContext` inside a `ToolbarSection` has a unique `name` and the
  section's `activeContext` matches one of them.
- `ToolbarFanOutItem` and `ToolbarFolder` fan away from the nearest screen edge.
- Slot names match between the `ToolbarSlot` and its consumer, and both sit
  inside the same `ToolbarSlotProvider`.
- Lint and the TypeScript build pass.
