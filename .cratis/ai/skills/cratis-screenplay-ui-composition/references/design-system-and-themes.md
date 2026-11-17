<!-- cratis-ai-managed: skills/cratis-screenplay-ui-composition/references/design-system-and-themes.md -->
# Design system and themes

## Contents

- The split: design system, theme, profile
- A design system and its default theme
- Tokens, components, words and rules
- Components are the rendering contract
- A second theme and a profile that selects it
- Which theme and mode a profile gets
- Deprecated `appearance` members
- Diagnostics
- Reading and editing the design
- Checklist

Requires a Screenplay release that carries design systems (6.3.0 or later; check `screenplay --version`). Earlier compilers reject `design system` and a `theme` with `implements`. The `ui profile` that selects a theme and the shell are in [ui-profile-and-theme.md](ui-profile-and-theme.md).

## The split: design system, theme, profile

| Construct | Role | How many |
| --- | --- | --- |
| `design system` | The contract: modes, tokens and what each is for, components built from tokens, words, contrast rules. No values. | Exactly one per application (a second is `PLAY0917`) |
| `theme ... implements` | An implementation: palette, token values, per-mode values. Never redefines components. | Several; one is the design system's `default theme` |
| `ui profile` | Picks a theme with `theme <Name>`; without one it is drawn in the `default theme`. `appearance` keeps only `color scheme`, which picks the mode. | One per target |

A theme with only `compatible with` lines and no `implements` is a compatibility-only theme and keeps working; a profile may select it only when the application declares no design system.

## A design system and its default theme

```screenplay
design system ShopDesign
  modes light, dark
  default theme Daylight

  tokens
    color
      surface    "page background"
      text       "body text"
      primary    "primary action"
      on-primary "text on a primary action"
    space
      md         "default gap between elements"
    radius
      control    "inputs and buttons"
    font
      sans       "interface text"

  components
    Button
      parts text, background, radius
      variant primary
        text       = color.on-primary
        background = color.primary
        radius     = radius.control
        state disabled
          text       = color.text
          background = color.surface

  words
    voice
      tone = "Plain and exact"
    naming
      customer = "Client"

  rules
    contrast text on background at least 4.5

theme Daylight
  implements ShopDesign
  compatible with core

  palette
    indigo = "#4338CA"
    white  = "#FFFFFF"
    ink    = "#111827"

  values
    space.md       = "12px"
    radius.control = "8px"
    font.sans      = "inter"

  light
    color.surface    = palette.white
    color.text       = palette.ink
    color.primary    = palette.indigo
    color.on-primary = palette.white

  dark
    color.surface    = palette.ink
    color.text       = palette.white
    color.primary    = palette.indigo
    color.on-primary = palette.white
```

- `default theme` is required (`PLAY0927`): a profile without a `theme` line resolves to it.
- A theme assigns every `color` token a value in every mode, from `values` or from the mode block (`PLAY0921`). Other categories need a value where a component uses the token (`PLAY0922`).
- A value is a quoted literal, `palette.<name>` or another token (`<category>.<name>`). Reference loops are `PLAY0923`.

## Tokens, components, words and rules

- Categories are closed: `color`, `space`, `size`, `radius`, `border`, `shadow`, `font`, `type`, `motion`, `opacity`, `z` (anything else is `PLAY0916`). A token is `<name> "<what it is for>"`; the description is the contract a theme author reads. Address a token as `<category>.<name>`.
- A component declares `parts` from `text`, `background`, `border`, `radius`, `height`, `padding`, `shadow`, `font`, then `variant`s assigning each part one token of a category the part takes (`background = space.md` is `PLAY0924`). `state` (`default`, `hover`, `focus`, `active`, `disabled`, `selected`) overrides only what changes.
- `words` holds `voice` and `naming` as `<key> = "<text>"` lines, once each; they travel with the design system.
- The one rule form is `contrast <part> on <part> at least <ratio>`. The compiler checks it for every theme, mode, variant and state except `disabled`, and warns `PLAY0928` naming the pair. Only hex and `rgb(...)` colors are measured; a named color is not checked.
- Modes belong to the design system, never to a theme; a mode block for an undeclared mode is `PLAY0918`.

## Components are the rendering contract

A renderer never reads a token by name. A component reads its look from its **anatomy**: "my `primary` variant's `background` in `hover`". The design system's `components` section is therefore what a renderer draws from, and the token names are the product's own: the categories are closed, the names inside them are free, and no renderer expects `color.primary` or any other name to exist.

- Declare, for each component, exactly the `parts`, `variant`s and `state`s it really has; a part, variant or state is a promise that a renderer can draw it. Shell and chrome (page, shell, sidebar, menu, card, text, button, input, status chip) are components like any other, so surfaces and text come from anatomy as well.
- **Link by name.** A design-system component finds the component that renders it by its name, through the profile's packages (the last package wins), compared without regard to case or separators: `StatusChip` finds `statusChip`. Name design-system components after the component they stand for and no override is needed.
- **Override with a mapping.** When names differ, Scene's component mapping states the target explicitly (`maps to <Package.Component>`). It is a Scene design-system mapping, not a `.play` line: the Screenplay `design system` has no `maps to`, so never write one in a model.
- **Variant to property.** The chosen variant reaches the component through its `variant` property, else `emphasis`, or through the property its anatomy names; a mapping may also state the property and the value each variant takes when the names differ (`variantProperty`, `variantValues`).
- **Mismatches are reported, never hidden.** Linking reports a component no package renders (`componentUnbound`), one that declares no anatomy (`componentWithoutAnatomy`), and a part, variant or state it does not support (`partUnsupported`, `variantUnsupported`, `stateUnsupported`), or several variants with no property to choose by (`variantPropertyMissing`). A component that does not restyle is diagnosed from these, not by adding raw values to a screen.
- **What the renderer emits.** Each part becomes a custom property per mode, `--ds-<component>-<variant>-<part>` and, for a state, `--ds-<component>-<variant>-<part>-<state>` (kebab-cased); raw token roles are `--ds-token-<category>-<name>` and are for blueprints authored against one design system. A package component reads only anatomy.

Scene Design grounds an agent's choices in these components and refuses what they do not declare: [scene-design.md](scene-design.md).

## A second theme and a profile that selects it

```screenplay
design system ShopDesign
  modes light, dark
  default theme Daylight

  tokens
    color
      surface    "page background"
      text       "body text"
      primary    "primary action"
      on-primary "text on a primary action"
    space
      md         "default gap between elements"
    radius
      control    "inputs and buttons"
    font
      sans       "interface text"

  components
    Button
      parts text, background, radius
      variant primary
        text       = color.on-primary
        background = color.primary
        radius     = radius.control
        state disabled
          text       = color.text
          background = color.surface

  words
    voice
      tone = "Plain and exact"
    naming
      customer = "Client"

  rules
    contrast text on background at least 4.5

theme Daylight
  implements ShopDesign
  compatible with core

  palette
    indigo = "#4338CA"
    white  = "#FFFFFF"
    ink    = "#111827"

  values
    space.md       = "12px"
    radius.control = "8px"
    font.sans      = "inter"

  light
    color.surface    = palette.white
    color.text       = palette.ink
    color.primary    = palette.indigo
    color.on-primary = palette.white

  dark
    color.surface    = palette.ink
    color.text       = palette.white
    color.primary    = palette.indigo
    color.on-primary = palette.white

theme Square
  implements ShopDesign

  palette
    indigo = "#4338CA"
    white  = "#FFFFFF"
    ink    = "#111827"

  values
    space.md       = "12px"
    radius.control = "0px"
    font.sans      = "inter"

  light
    color.surface    = palette.white
    color.text       = palette.ink
    color.primary    = palette.indigo
    color.on-primary = palette.white

  dark
    color.surface    = palette.ink
    color.text       = palette.white
    color.primary    = palette.indigo
    color.on-primary = palette.white

ui profile Desktop
  target platform web
  appearance
    color scheme dark

ui profile Compact
  target platform web
  theme Square
  appearance
    color scheme light
```

`Desktop` has no `theme` line, so it gets `Daylight` (the default) in the `dark` mode. `Compact` overrides with `Square` and draws in `light`. The selected theme must implement the application's design system (`PLAY0926`). With no `color scheme`, or one that is not a mode (`system`), the first mode is used.

## Which theme and mode a profile gets

`DesignSystemResolver` in `Cratis.Screenplay.Design` offers `SelectTheme`, `SelectMode` and `Resolve(application, profile)`; the result is a flat `<category>.<name>` map with palette and token references followed, plus each component's variants and states merged over the variant's parts. A host reads that, not the syntax.

## Deprecated `appearance` members

`appearance` keeps only `color scheme`. `accent`, `corners` and `font` still read but warn `PLAY0929`; move the value into the theme:

| Was in `appearance` | Becomes, in the theme |
| --- | --- |
| `accent "#4F46E5"` | a `color` token value, such as `color.primary = "#4F46E5"` |
| `corners round` | a `radius` token value, such as `radius.control = "8px"` |
| `font inter` | a `font` token value, such as `font.sans = "inter"` |

Do not carry raw colors, radii or fonts in a profile: declare the token in the design system and give it a value in each theme.

## Diagnostics

| Code | Severity | Meaning |
| --- | --- | --- |
| `PLAY0916` | Error | Malformed `design system`: unknown section, token category, component part or state, bad line, a part the component does not declare, or a section twice. |
| `PLAY0917` | Error | A second `design system`. |
| `PLAY0918` | Error | A theme block for a mode the design system does not declare. |
| `PLAY0919` | Error | A mode, token, component, variant, part, palette entry, value or word declared twice. |
| `PLAY0920` | Error | A theme assigns a token the design system does not declare. |
| `PLAY0921` | Error | An implementing theme gives a `color` token no value in a declared mode. |
| `PLAY0922` | Error | A design reference names something undeclared, or a token with no value. |
| `PLAY0923` | Error | Design references loop. |
| `PLAY0924` | Error | A part takes a token of a category it cannot take. |
| `PLAY0925` | Error | A theme `implements` an undeclared design system. |
| `PLAY0926` | Error | A profile selects a theme that does not implement the design system. |
| `PLAY0927` | Error | No `default theme`, or it is undeclared or does not implement the design system. |
| `PLAY0928` | Warning | A theme and mode resolve a text/background pair below a `contrast` rule (`disabled` not checked). |
| `PLAY0929` | Warning | `accent`, `corners` or `font` in `appearance` is deprecated. |
| `PLAY0930` | Error | A theme's `implements`, `palette`, `values` or mode block is malformed or repeated. |

The executable semantic model defers the design system as UI configuration (`PLAY0269` on binding); it is not an error.

## Reading and editing the design

The MCP `ui-profiles` read tool (no new tool) also returns the design system, every theme with its palette, values and per-mode values, each theme's resolved tokens and components per mode, the theme and mode each profile resolves to (`resolvedTheme`), the diagnostics `PLAY0916` to `PLAY0930` and the closed vocabulary of categories, parts and states. Edit with `propose-ast` on `DesignSystemSyntax`, `DesignTokenCategorySyntax`, `DesignTokenSyntax`, `DesignComponentSyntax`, `DesignVariantSyntax`, `DesignStateSyntax`, `DesignAssignmentSyntax`, `DesignWordsSyntax`, `DesignContrastRuleSyntax`, `ThemeModeValuesSyntax` and `ThemeSyntax` (`implements`, `palette`, `values`, `modeValues`); the compiler refuses an invalid edit with its code. Node details: [model-authoring MCP tools](../../cratis-screenplay-model-authoring/references/mcp-tools.md).

Source: [Design system](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/design-system.md) and [Theme](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/theme.md).

## Checklist

- [ ] One `design system` with a `default theme`; each selected theme implements it and sets every `color` token in each mode; no profile carries `accent`, `corners` or `font`.
- [ ] Each component declares only the parts, variants and states a renderer draws, and is named after the component that renders it; a name that differs is a Scene mapping, not a `.play` line.
