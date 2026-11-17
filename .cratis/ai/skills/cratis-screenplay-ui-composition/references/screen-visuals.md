<!-- cratis-ai-managed: skills/cratis-screenplay-ui-composition/references/screen-visuals.md -->
# Sketches, mockups and images on a screen

Early in modeling a screen is often a quick sketch, a screenshot of the system being replaced or a
hand-written HTML page. Keep them with the screen so they travel with the model and reach whoever
builds it. Requires Screenplay 6.7.0 or later (`screenplay --version`).

The compiler warns `PLAY0952` while a named file is missing, so this fence asserts that code; a
model whose files exist beside it compiles clean.

```screenplay expect PLAY0952
module Ordering
  feature Orders
    slice StateView OpenOrdersView
      screen OpenOrdersScreen
        sketch Screens/open-orders.sketch.json
        mockup Screens/open-orders/index.html
        mockup Screens/open-orders/confirm.html
        image Screens/open-orders-today.png
```

| Line | Holds |
| --- | --- |
| `sketch <path>` | A drawing of the screen as vector drawing data, such as a board sketch export. |
| `mockup <path>` | One HTML/CSS page of a coded mockup. Repeat the line per page, in reading order. |
| `image <path>` | A screenshot or design export: PNG, JPG, GIF, SVG or WebP. |

- A screen can carry any number of each, beside or instead of its structure.
- Paths are relative to the model root, like the `file` directive, and the files live in the
  repository next to the `.play` files.
- They are authoring metadata. The printer keeps them in the order written, after `description`,
  `file` and `fits`; the AST and MCP editing tools carry them; they never change what the screen
  does and never reach the executable semantic model.
- Do not use them to replace a composed screen once the screen is designed: they record the origin
  or the intent, the `data`, `action` and layout lines state the contract.

| Code | Severity | Meaning |
| --- | --- | --- |
| `PLAY0950` | Error | A `sketch`, `mockup` or `image` line names no file. |
| `PLAY0951` | Warning | An `image` is not PNG, JPG, GIF, SVG or WebP, or a `mockup` is not an HTML page (`.html`, `.htm`). |
| `PLAY0952` | Warning | The file does not exist beside the model, or its path leaves the model root. |

`PLAY0952` needs the files, so the product reports it where a model root is opened (the MCP server,
`open-workspace`); the standalone tool reports it for a folder it compiles, as the fence above shows.
