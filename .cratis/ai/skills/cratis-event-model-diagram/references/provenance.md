<!-- cratis-ai-managed: skills/cratis-event-model-diagram/references/provenance.md -->
# Provenance

## Adapted closely (Martin Dilger and Nebulit GmbH)

Source: https://github.com/Nebulit-GmbH/agentic-engineer at commit `07b0f30648d663cb588d7e2c7aa031af9dfc21f2`,
by Martin Dilger and Nebulit GmbH (https://nebulit.de), used with the explicit consent of Martin Dilger
and Nebulit GmbH. The notice is in this skill's `LICENSE`.

| Source file | Used in | How |
|---|---|---|
| `.claude/skills/eventmodeling-core-rules/SKILL.md`: "Connections Read Forward" and "Updating a Read Model That Already Feeds a Screen" | `SKILL.md` "Connections read forward" | Adapted closely; board columns, cells, linked copies and `link_element` dropped; the later-state read model becomes a later `rmo` frame in the Mermaid diagram |

Not adopted: lanes and the four-row column layout, placement mechanics, single-issuer commands (Mermaid frames
carry no such rule, and Screenplay commands may have several origins).

## Other lineage

- Mermaid `eventmodeling` diagram type: https://mermaid.js.org/syntax/eventmodeling.html.
- Event Modeling (Adam Dymitruk) and Martin Dilger, *Understanding Eventsourcing*: the timeline as left-to-right business flow.

### Eventmodelers Build-Kits

Source: https://github.com/Nebulit-GmbH/Eventmodelers-Build-Kits, by Martin Dilger and Nebulit GmbH
(https://nebulit.de), used with the explicit consent of Martin Dilger and Nebulit GmbH.

| Source skill | Used in | How |
|---|---|---|
| `detect-model-drift` | `SKILL.md` step 4 | idea, own wording: the last commit touching the artifact is the baseline, and behavior commits since then are walked oldest first |
