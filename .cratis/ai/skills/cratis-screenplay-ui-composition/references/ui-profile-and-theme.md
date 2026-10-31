<!-- cratis-ai-managed: skills/cratis-screenplay-ui-composition/references/ui-profile-and-theme.md -->
# UI profile and theme

## Contents

- The shell: `root template`
- `ui profile` and `theme`
- Reading and editing the shell

This reference holds the `ui profile` and `theme` excerpts and their rules: the root template that names the application shell, shell options, appearance, the new screens default, platforms, sizes, package order and theme compatibility. Requires Screenplay 4.200.0 or later (`screenplay --version`) for the diagnostics `PLAY0790` to `PLAY0799`; 4.197.0 reads the syntax.

## The shell: `root template`

A `ui profile` names the template the whole application is drawn in with `root template`. Every screen's template chain is placed in the
shell's primary outlet (`content`). The reference is the one a `template` assignment uses: an own `layout` or application-scope
screen template by name, or a template a blueprint ships, `blueprint <Blueprint> <Name>`, with an optional `version "<range>"` pin.
`ui profile` is the only place that selects the shell; a screen never names it.

```screenplay
layout AppShell
  category application
  topbar
  side
  content

exposure for AppShell
  property topbar.showFooter label "Footer"

module Workspaces
  screen template Sheet
    category Page
    scopes feature
    content

ui profile Desktop
  target platform web
  target size regular
  theme Nordic

  root template AppShell
    option showFooter true

  identity
    name "Contoso CRM"
    logo initials "CC"

  appearance
    color scheme dark
    accent "#4F46E5"
    corners round
    font inter

  new screens use template Sheet

  packages
    Cratis.Components

theme Nordic
  compatible with Cratis.Components
```

A shell from a blueprint lists the blueprint under `packages` (the blueprint's name is a package) and may pin a version:

```screenplay
ui profile Web
  packages
    Cratis.Blueprint.Default

  root template blueprint Cratis.Blueprint.Default AppShell version "^4.5"
  new screens use template blueprint Cratis.Blueprint.Default Detail
```

- `root template`: at most one per profile. The shell is a `layout`, or a screen template that declares `scopes application` and fits no slot. It
  needs a `content` slot, or no screen has a place in it.
- `option <name> <value>`: a configurable item the shell exposes (an own shell exposes the properties of its `exposure for <Shell>`).
  The value is a bare word, a quoted string, a number, `true` or `false`. Set each name once.
- `identity`: what the application calls itself and the mark it wears. `name "<text>"` (the application's own name when left out),
  `logo initials "<AB>"` (1-2 characters), `logo icon <icon>` (`box`, `layers`, `home`, `users`, `receipt`, `package`, `database` or `palette`) or
  `logo image "<data:image/png|jpeg|svg+xml|webp;base64,...>"` (400 KB at most; the logo travels inside the document, never as a file beside it),
  and `show name true|false`. One block per profile, each line once. Give each application its own `identity` so Studio and Stage draw its logo.
- `appearance`: `color scheme` is `light`, `dark` or `system`; `accent` is a quoted `#RGB`, `#RRGGBB` or `#RRGGBBAA`; `corners` is `sharp`,
  `default` or `round`; `font` is an id of Scene's font catalog (`inter`, `ibm-plex-sans`, `source-serif-4`, `system-ui`). One block per profile.
- `new screens use template`: the template a new screen starts from in a designer. It is an authoring default, it changes no existing
  screen, and it has to be able to start a screen in a feature (not a layout or an application-scope root; a `scopes` list that leaves
  out `feature` is refused).
- A version range only applies to a blueprint template; `*`, an exact version or a `^`, `~`, `>=`, `>`, `<=` or `<` range.

### Errors and fixes

| Code | Severity | Cause | Fix |
| --- | --- | --- | --- |
| `PLAY0788` | Error | A `root template` or `new screens use template` line is malformed, or it pins a version on an own template | Write `[blueprint <B>] <Name> [version "<range>"]`; drop the version on an own template |
| `PLAY0789` | Error | A malformed `identity` or `appearance` line, a repeated line, or two `identity` / `appearance` blocks | `color scheme <word>`, `accent "<colour>"`, `corners <word>`, `font <id>`, `name "<text>"`, `logo initials|icon|image <value>`, `show name <true|false>`, each once, one block each |
| `PLAY0790` | Error | The template is not declared in the document (or the blueprint does not ship it) | Declare `layout <Name>` or `screen template <Name>`, or take it from a blueprint |
| `PLAY0791` | Warning | The profile still selects its shell with the legacy `layout <Name>` line | Write `root template <Name>`; remove the `layout` line when a `root template` is present (the quick fix does either) |
| `PLAY0792` | Error | The blueprint is not listed in the profile's `packages` (or not installed) | Add it under `packages` |
| `PLAY0793` | Warning | The pinned `version` range is not satisfied by the installed blueprint | Move the pin or update the blueprint |
| `PLAY0794` | Error | The root template cannot be the shell: a dialog or ordinary screen template, a slot-fitting template, `scopes` without `application` | Choose a layout or an application-scope template |
| `PLAY0795` | Error | The root template has no `content` slot | Add the line `content` to it |
| `PLAY0796` | Warning | An `option` the shell does not expose | Use a name the shell exposes, or add the property to its `exposure` |
| `PLAY0797` | Error | An invalid color scheme, accent, corners or identity value | `light`, `dark` or `system`; a quoted hex colour; `sharp`, `default` or `round`; 1-2 initials, a listed icon, an image data URI of at most 400 KB |
| `PLAY0798` | Error | More than one `root template` in one profile | Keep one |
| `PLAY0799` | Error | The `new screens use template` default cannot start a screen in a feature | Name a feature-scope screen template |

What a blueprint ships is only known where the blueprint is installed, so its checks (`PLAY0790` for a template it lacks, `PLAY0793`,
the options and outlets of its templates) come from the host that has the catalog (Stage, the `cratis` CLI, the MCP server); the compiler
decides everything the document alone decides.

### Negative example: the legacy `layout` line

`layout <Name>` inside a profile still reads, but it is the old way to name the shell. Do not write it in a new model:

```screenplay excerpt
layout AppShell
  category application
  topbar
  content

ui profile Desktop
  layout AppShell
```

The canonical form is `root template AppShell`:

```screenplay
layout AppShell
  category application
  topbar
  content

ui profile Desktop
  root template AppShell
```

## `ui profile` and `theme`

Excerpt: `AppShell` is the layout above.

```screenplay excerpt
ui profile Desktop
  target platform web
  target size regular
  root template AppShell
  theme Nordic
  packages
    Cratis.Components

theme Nordic
  compatible with Cratis.Components
```

- `target platform` takes a comma-separated list. `web`, `ios` and `android` are
  the documented examples; ⚠️ **the parser accepts any identifier** — there is no
  enforced list, so a typo is silent.
- `target size` is documented as `compact`, `regular` or `expanded`; ⚠️ again any
  identifier parses.
- `packages` are listed **in override-priority order** — a later package's `Button`
  shadows an earlier one's — and `core`, the built-in vocabulary, is always the
  final fallback.
- Component package metadata is owned by the package/runtime catalog; the model
  selects packages and icons, never copying metadata into `.play`.
- `compatible with` lists the packages a theme actually supports, each at most
  once. A profile selecting a theme not declared compatible with one of its own
  packages gets a compile-time **warning**.

## Reading and editing the shell

`ui-profiles` (MCP, Screenplay 4.201.0 or later) returns every profile's root template, options, appearance and new screens default with the shell diagnostics, and the own templates that could be the root template or the new screens default. Edit with `propose-ast` on `RootTemplateSyntax`, `ShellOptionSyntax` (`syntaxKind` is the value's kind), `ProfileAppearanceSyntax` and `NewScreenTemplateSyntax`; an invalid reference is refused with its code.
