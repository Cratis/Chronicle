<!-- cratis-ai-managed: skills/cratis-components-styling/references/renderer-adapters.md -->
# Renderer adapters

This reference holds the optional renderer adapter packages, their nine slots and version rules.

## Renderer adapters — when a vendor look is wanted

The default renderer is built in; omit `library` on the provider. Three optional
adapter packages exist — `@cratis/components.mui` (MUI 9),
`@cratis/components.primereact` (PrimeReact 11), `@cratis/components.primereact10`
— and each adapts exactly **nine primitive slots**: button, icon-button, text
input, text area, checkbox, radio, switch, progress bar (`common.progress`, not
`common.progressBar`), surface. Install and upgrade each adapter at **exactly
the same version** as `@cratis/components`; Node >=22 is required. The broader
renderer contract also has an atomic `common.tooltip` slot accepting any React
node, but it is not one of the nine stable presentation slots. Pass the
adapter's manifest to the provider's `library` prop. Dialogs, dropdowns, tables,
date pickers, tooltips, paginators, toasts and steppers stay Components-owned
regardless; an adapter never restores a vendor's public API, and the vendor's
own provider, theme and license remain the application's, outside Components.
Reach for an adapter only when the product genuinely wants that vendor's look on
those nine controls — mapping tokens is almost always the smaller change.
