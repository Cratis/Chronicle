<!-- cratis-ai-managed: skills/cratis-screenplay-ui-composition/references/deliverable-identity.md -->
# A deliverable app declares its identity, titles and labels

## Symptom and fix

Symptom: a rendered app titled "Cratis Stage" whose buttons and dialogs show raw command names. The renderer
defaults to those when the model is silent, so declare, in every deliverable app:

- the layout's `logo image "<path>" title "<text>"` inside a navigation outlet (the
  application title the shell shows; `logo initials` is only valid in a profile's `identity`);
- a `ui profile` with `root template`, shell `option` lines such as `option showFooter false`, and an `identity`
  block (`name`, `logo initials`);
- a `title "<text>"` in each screen and a navigation item `<id> title "<text>" target <Module.Feature>` per screen;
- `label "<text>"` on **every** `action`.

## Example

```screenplay excerpt
layout AppShell
  category application
  content page routed primary

  outlet sidebar of navigation
    logo image "/assets/shop.svg" title "Shopfront"
    default organizations title "Organizations" target Crm.Organizations

ui profile Desktop
  target platform web
  root template AppShell
    option showFooter false
  identity
    name "Shopfront"
    logo initials "SF"
```

```screenplay excerpt
screen Organizations
  data OrganizationOverview[] via query AllOrganizations
  title "Organizations"
  action RegisterOrganization
    label "New organization"
```
