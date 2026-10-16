<!-- cratis-ai-managed: skills/cratis-screenplay-ui-composition/references/localized-strings.md -->
# Localized strings

This reference holds the `.strings` file format and where `$strings` references are accepted.

## Localized strings

Localized text lives in companion `.strings` files — `MySystem.play` pairs with
`MySystem.<locale>.strings`, line-based, dotted keys, `=` assignment, `//`
comments. `{placeholder}` tokens are kept verbatim for runtime substitution.

Reference them with `$strings.<dotted.key>`, unquoted, anywhere a literal is
accepted in: a validation rule's `message`; a screen action, table column or
summary field `label`; a screen or section `title`; a contribution `label`; and a
form field `label`. The value is stored as the literal text `$strings.<key>`, and
the printer emits it unquoted so a round trip preserves it.
