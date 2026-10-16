<!-- cratis-ai-managed: skills/cratis-screenplay-ui-composition/references/layout-arrangement.md -->
# Layout arrangement

This reference holds the responsive `flow` and pixel-precise `freeform` arrangement forms.

## `arrangement` — responsive `flow` or pixel-precise `freeform`

The size-class matrix is **2×2**: width `compact` or `regular`, height `compact`
or `regular` — phone landscape, phone portrait, desktop short, desktop tall.

**`arrangement flow`** nests slots under `row`, `column` or `grid` containers.

- A container may declare `gap <n>`.
- A slot leaf takes `width <n>`, `height <n>`, `grow`, and `span <n>` for grid tracks.
- `when width <class>[, height <class>]` or `when height <class>` **replaces the
  entire tree** for that condition — it is not a partial override.

**`arrangement freeform`** declares one variant per matrix point. Excerpt, inside
the `layout` above in place of its `arrangement flow`:

```screenplay excerpt
arrangement freeform
  variant width regular, height regular
    place navigation at 0,0 size 240,fill
    place content    at 240,0 size fill,fill
  variant width compact, height regular
    place navigation hidden
    place content    at 0,0 size fill,fill
```

`place <Slot> at <x>,<y> size <w>,<h>` where either dimension may be `fill`, or
`place <Slot> hidden` to drop the slot from that variant entirely.
