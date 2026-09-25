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
| `@cratis/components` | `4.6.0` | its package manifest, `Source/Toolbar/*` (exports and props), `Documentation/Toolbar/*`, `Documentation/Common/icon.md` |
| `react-icons` | `5.7.0` | dependency of `@cratis/components@4.6.0` — the icon source the examples use |
| `react` | `^19.0.0` | peer of `@cratis/components@4.6.0` |

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
import { FaArrowPointer, FaPencil, FaVectorSquare } from 'react-icons/fa6';
import { Toolbar, ToolbarButton } from '@cratis/components/Toolbar';

export const DrawingToolbar = () => (
    <Toolbar>
        <ToolbarButton icon={<FaArrowPointer />} title='Select' />
        <ToolbarButton icon={<FaPencil />} title='Draw' />
        <ToolbarButton icon={<FaVectorSquare />} title='Rectangle' />
    </Toolbar>
);
```

`icon` is the Components `Icon` type: a **React node** (preferred — the examples
below take theirs from `react-icons/fa6`, which Components itself depends on, and
omit the import line after this first one) or a complete, consumer-owned
icon-font class string. Components installs **no icon font**: a string is rendered
on an `<i>` unchanged, so the product must load that stylesheet and pass every
class the font needs. A custom SVG component works directly:
`<ToolbarButton icon={<CircleIcon />} title='Circle' />`.

## Step 2 — Active state

```tsx
const [activeTool, setActiveTool] = useState('select');

<Toolbar>
    <ToolbarButton icon={<FaPencil />} title='Draw'
        active={activeTool === 'draw'} onClick={() => setActiveTool('draw')} />
    <ToolbarButton icon={<FaVectorSquare />} title='Rectangle'
        active={activeTool === 'rect'} onClick={() => setActiveTool('rect')} />
</Toolbar>
```

## Step 3 — Separators and horizontal layout

Give `ToolbarSeparator` the same `orientation` as its `Toolbar`, and move
tooltips out of the bar's way on a horizontal toolbar:

```tsx
<Toolbar orientation='horizontal'>
    <ToolbarButton icon={<FaMinus />} title='Zoom out' tooltipPosition='bottom' onClick={() => setZoom(z => z - 10)} />
    <ToolbarButton text={`${zoom}%`} title='Reset zoom' tooltipPosition='bottom' onClick={() => setZoom(100)} />
    <ToolbarButton icon={<FaPlus />} title='Zoom in' tooltipPosition='bottom' onClick={() => setZoom(z => z + 10)} />
    <ToolbarSeparator orientation='horizontal' />
    <ToolbarButton icon={<FaCircleQuestion />} title='Help' tooltipPosition='bottom' />
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
    <ToolbarButton icon={<FaArrowPointer />} title='Select' />
    <ToolbarSection activeContext={mode}>
        <ToolbarContext name='drawing'>
            <ToolbarButton icon={<FaPencil />} title='Draw' />
            <ToolbarButton icon={<FaVectorSquare />} title='Rectangle' />
        </ToolbarContext>
        <ToolbarContext name='text'>
            <ToolbarButton icon={<FaAlignLeft />} title='Align left' />
            <ToolbarButton icon={<FaAlignCenter />} title='Align center' />
        </ToolbarContext>
    </ToolbarSection>
    <ToolbarButton icon={<FaRotateLeft />} title='Undo' />
</Toolbar>
```

## Step 5 — Fan-out and folders

`ToolbarFanOutItem` slides a strip of extra buttons out of its trigger. It is
the one component whose label prop **is** called `tooltip`, and both `icon` and
`tooltip` are required.

```tsx
<ToolbarFanOutItem icon={<FaShapes />} tooltip='Shapes' fanOutDirection='left'>
    <ToolbarButton icon={<FaVectorSquare />} title='Rectangle' />
    <ToolbarButton icon={<FaCircle />} title='Circle' />
</ToolbarFanOutItem>
```

`fanOutDirection` is `'right'` (default), `'left'`, `'up'`, or `'down'` — fan
away from the screen edge the toolbar sits against.

`ToolbarFolder` opens a larger panel of buttons as a grid or a list:

```tsx
<ToolbarFolder icon={<FaShapes />} title='Tools' mode='list'>
    <ToolbarButton icon={<FaPencil />} title='Draw' />
    <ToolbarButton icon={<FaEraser />} title='Erase' />
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
            <ToolbarButton icon={<FaArrowPointer />} title='Select' />
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
