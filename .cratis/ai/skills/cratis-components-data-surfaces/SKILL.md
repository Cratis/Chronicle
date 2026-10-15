---
name: cratis-components-data-surfaces
description: Compose Cratis Components PivotViewer exploration, TimeMachine historical snapshots, Canvas pan/zoom boards and public Chat conversation/sidebar surfaces. Use when a table is not the right presentation, when supplying Pixi-backed content, or when binding host-owned chat data and actions. Do not use for DataPage lists, backend event replay, chat persistence, canvas tool palettes or Scene authoring.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-components-data-surfaces/SKILL.md -->

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Data beyond a table

## Verified product sources

All API and behavior claims below use **Cratis/Components v4.26.4**.

| Package | Version | Source |
| --- | --- | --- |
| `@cratis/components` | `4.26.4` | `Source/PivotViewer/types.ts:67-112,145-187`; `Source/TimeMachine/{TimeMachine.tsx:19-65,types.ts:8-34}`; `Source/Canvas/{Canvas.tsx:38-104,CanvasItem.tsx:8-38,CanvasHandle.ts:7-21}`; `Source/Chat/{index.ts:4-40,ChatConversation.tsx:78-180,ChatSidebar.tsx:86-203}` |
| `pixi.js` | `^8.20.0` optional peer | `Source/package.json:319-340`; required when using Canvas or PivotViewer |
| React | `^19.0.0` peer | `Source/package.json:319-340` |

Subpath exports: `Source/package.json:60-69,155-159,178-182`; each area's `index.ts` exports the types used below. Load **cratis-components-styling** for CSS/provider setup and **cratis-components-accessibility** for localized names. For tabular queries use **cratis-arc-react-page**; for a board tool palette use **cratis-components-toolbar**.

These are **presentation contracts**. The host owns loading, access control, data acquisition and persistence. A historical slider is not event replay; a chat send callback is not a durable write.

## Choose the surface

| Surface | Fit | Avoid assuming |
| --- | --- | --- |
| `PivotViewer` | Faceted card exploration of an already-loaded collection | Server paging, JSX card rendering or controlled selection |
| `TimeMachine` | Supplied historical snapshots and event descriptions | Fetching history, rebuilding a read model or a controlled current index |
| `Canvas` | A pan/zoom board combining DOM items and optional Pixi objects | That moving the camera persists item positions |
| `ChatConversation` / `ChatSidebar` | Host-owned messages, topic selection and actions | Automatic message persistence, authorization or topic naming |

Keep Canvas/PivotViewer behind their subpath imports so ordinary pages do not acquire a Pixi import edge. Install/deduplicate the optional `pixi.js` peer for these areas, including a DOM-only Canvas: `Canvas.tsx:176` initializes Pixi through `usePixiApplication`. Do not rely on the word "optional" to omit it when importing those areas.

## 1. PivotViewer: data and text cards, not React cards

`data`, `dimensions` and `cardRenderer` are required. Each dimension has `key`, `label`, `getValue`; optional `formatValue` and `sort` control group presentation. **`cardRenderer` returns structured text, not JSX**. `detailRenderer(item, onClose)` may return a React node (`PivotViewer/types.ts:67-78,145-163`).

This component accepts loaded domain data; its labels come from the host's localization layer:

```tsx
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { PivotViewer } from '@cratis/components/PivotViewer';

interface CatalogItem {
    id: string;
    name: string;
    category: string;
}

interface CatalogExplorerProps {
    items: CatalogItem[];
    categoryLabel: string;
    loadingLabel: string;
    isLoading: boolean;
}

export const CatalogExplorer = (props: CatalogExplorerProps) => (
    <PivotViewer<CatalogItem>
        data={props.items}
        dimensions={[{ key: 'category', label: props.categoryLabel, getValue: item => item.category }]}
        getItemId={item => item.id}
        cardRenderer={item => ({ title: item.name, values: [item.category] })}
        searchFields={[item => item.name]}
        isLoading={props.isLoading}
        loadingLabel={props.loadingLabel}
    />
);
```

Other substantial props (`types.ts:93-112,145-185`):

- `filters` accepts keyed facets with `label` and `getValue`. Optional `options` are `{ key, label, value, count }`; otherwise they are computed from data. Numeric `type='number'` supports a range/histogram with `buckets`; `renderEditor` replaces a facet editor.
- `defaultDimensionKey` initializes grouping. Group/filter/selection state is internal; there is no `selectedItem`/`onSelectedItemChange` prop at this tag (`PivotViewer.tsx:54-112`).
- `searchFields` are property accessors. Use direct property paths, not computed expressions: the implementation extracts paths from accessor source (`types.ts:23-29`, `utils/search.ts`).
- `emptyContent`, `isLoading`, `loadingLabel`, `labels` and `colors` control presentation. Use the exported `PivotViewerLabels` shape for localization; do not put DOM markup in a text-card result.

Bound the dataset in the host. A worker and virtualized rendering do not make loading an unbounded collection a safe query shape (`PivotViewer.tsx:152-165`). Test actual browser rendering, resizing, zoom and keyboard behavior; a server-rendered shell does not exercise Pixi.

## 2. TimeMachine: prepare history before rendering

`Version` is `{ id: string, timestamp: Date, label: string, content: ReactNode, events?: Event[] }`. `Event` is `{ sequenceNumber: number, type: string, occurred: Date, content: Record<string, unknown> }`; it is a presentation DTO, not a Chronicle client object (`TimeMachine/types.ts:8-34`). Supply versions in the intended history order.

Binding excerpt for an existing view with `versions: Version[]`, a caller-owned `onVersionSelected(index)` callback and translated `timelineLabels: Partial<TimeMachineLabels>`:

```tsx
import { TimeMachine } from '@cratis/components/TimeMachine';

<TimeMachine
    versions={versions}
    currentVersionIndex={0}
    onVersionChange={onVersionSelected}
    labels={timelineLabels}
/>
```

**`currentVersionIndex` is initial-only**. Later prop changes do not replace internal selection. `onVersionChange(index)` reports timeline/wheel/previous/next selection; `scrollSensitivity` defaults to 50. The component offers read-model and event views over supplied data (`TimeMachine.tsx:19-65,73-125,133-145`). Do not promise an externally controlled scrubber or backend replay.

Load only authorized, redacted history. The presence of an event payload in this display is not permission to reveal it, and snapshot content is host-supplied React content. Exercise both modes and localization with the real DTOs.

## 3. Canvas: distinguish camera, DOM items and Pixi data

DOM content belongs in `Canvas` children. Position measured items with `CanvasItem x/y`, optional stable `id`, and optional `zIndex` on the item root (nested z-index cannot escape its stacking context). The item registry feeds minimap measurements (`CanvasItem.tsx:8-42,65-95`).

```tsx
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Canvas, CanvasItem } from '@cratis/components/Canvas';

export const Board = () => (
    <Canvas style={{ height: '32rem' }} showMinimap minimapWorldWidth={1600} minimapWorldHeight={1200}>
        <CanvasItem id='first-card' x={100} y={80}>
            <article>First card</article>
        </CanvasItem>
    </Canvas>
);
```

The literal card text is illustrative; use the host's approved/localized content. Give the board visible dimensions. For Pixi items, supply `items: T[]` extending `CanvasItemData` and `renderItem(item): PIXI.Container`; `onItemPointerDown` reports item activation, `onReady` receives `CanvasContext` (`Canvas.tsx:38-50`). Do not return React nodes from `renderItem`.

Camera/controls contract (`Canvas.tsx:51-104,119-160`, `CanvasHandle.ts:7-21`):

- `initialZoom` and `initialPan` are initial values, not a controlled camera. `onTransformChange(zoom, pan)` reports movement. Use `onHandleReady` and `CanvasHandle.smoothPanToWorld`/`smoothPanZoomToWorld` for navigation; `getContainerRect` and `getItemBounds` measure it.
- `minZoom`/`maxZoom` default to 0.1/5. `showControls` defaults true; `showMinimap` false. `minimapItems` can replace registry-derived rectangles.
- `controlsPlacement` is `'bottom-left' | 'bottom-right'`; `controlsLabels` and `controlsIcons` are typed override objects. `controlsFollowViewportInsets` defaults true, stepping aside by `--canvas-viewport-left/right`. Set false when the host already offsets the whole canvas, or the inset is applied twice.
- `readOnly` absorbs content interaction but leaves pan/zoom available. `backgroundDragPans=false` lets a product own background drag selection while wheel/middle-button/touch panning remain available.

Persist item edits in the host, not in camera callbacks. Browser-test gestures and dimensions. For jsdom tests provide a `ResizeObserver` substitute where required; Pixi integration still needs a real browser signal.

## 4. Public Chat: bind the host's messages and outcomes

The public `@cratis/components/Chat` exports **`ChatConversation`**, **`ChatSidebar`**, **`ChatSidebarForObservableQueries`**, **`ChatTopicList`** and their types. It does **not** export the Kit `Chat` component or expose a `Chat/Kit` package subpath (`Source/Chat/index.ts:4-40`, `Source/package.json`). **`Chat` is exported through `@cratis/components/Canvas`** via the Kit barrel (`Source/Canvas/index.ts:37-40`, `Source/Chat/Kit/index.ts:4-26`). Prefer the non-spatial Chat subpath for ordinary host-data conversations; importing Canvas brings its Pixi edge.

The Kit `Chat` has a different contract: required `messages`, `onSend(text)` and `onClose`; optional `variant`, `typingAuthors`, `currentUserId`, `onReact(messageId, emoji)`, `onAct(messageId)`, `canAct` and nested `labels`. Its messages use the Kit `ChatMessage` shape, not the public Chat subpath's message type. Reactions need both `currentUserId` and `onReact`. An optional `id` additionally publishes `ChatMessageAdded` over an available Arc messenger at send time, not after persistence succeeds (`Source/Chat/Kit/Chat.tsx:58-135`, `ChatMessage.ts`). Never count that notification as durable delivery, or mix `onSend` with the public conversation's `onSendMessage`.

The public `ChatMessage` has `id`, `topicId`, `authorId`, `body`, `timestamp: Date`, optional `mentions` and `metadata`. Identifiers use `ChatIdentifier`; resolve authors through `authorOf`, not an invented `authorName` property (`ChatMessage.ts:17-37`, `ChatConversation.tsx:78-127`).

Complete presentation wrapper using the real public types:

```tsx
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import {
    ChatConversation, ChatStatus,
    type ChatConversationLabels, type ChatMention, type ChatMessage
} from '@cratis/components/Chat';

interface ConversationProps {
    messages: ChatMessage[];
    status: ChatStatus;
    labels: ChatConversationLabels;
    send: (body: string, mentions: ChatMention[]) => void;
}

export const Conversation = (props: ConversationProps) => (
    <ChatConversation
        messages={props.messages}
        status={props.status}
        labels={props.labels}
        onSendMessage={props.send}
    />
);
```

`onSendMessage` reports the trimmed body and mentions; persistence is the host's responsibility. Supply fresh authorized data after command success. Handle failure visibly rather than adding a fabricated message that looks saved.

`ChatStatus.Ready/Loading/Failed/Unauthorized` distinguish query outcomes. Existing messages remain during loading/failure; unauthorized replaces them and disables the composer (`ChatStatus.ts:5-14`, `ChatConversation.tsx:85`). This is UI behavior, not backend enforcement.

Additional public contracts:

- `actions` are typed `ChatMessageAction<TMessage>[]`; `renderMessageExtra(message)` supplies reaction/failure/detail content under the body. `mentionCandidates` or `resolveMentionCandidates` supplies available mentions (`ChatConversation.tsx:121-180`).
- `ChatSidebar` requires `open`, `onClose`, `topics`, `messages`, and `onSendMessage(topicId, body, mentions)`. `topicActions` is separate from inherited message `actions`; `renderHeaderActions(openTopic)` belongs to the sidebar (`ChatSidebar.tsx:86-163`).
- `selectedTopicId` is controlled when provided: ID opens a topic, null shows the list, omission uses internal selection. `onTopicSelected` reports selection. `topicsStatus` and `messagesStatus` are separate; do not show one topic's cached messages while another topic is loading (`:123-142`).
- `onStartTopic` returns a created identifier or undefined, optionally in a promise. `onRequestTopicName` asks the host to name an unnamed topic after its first message. Neither creates/saves/names anything on its own (`:144-180`).
- The sidebar defaults non-modal, right edge, `24rem`; `position`, `width`, `modal`, `labels`, and typed `pt` customize it (`:182-203`).

## Verify the surface, not only the props

Test host orchestration separately from Components: loading/failed/denied vs empty, stable IDs, data ordering, and persistence errors. Browser-check interaction/focus and all visible/localized labels. Do not infer remote fetch, tenant isolation, history reconstruction or message delivery from a rendered surface or a passing type check.
