<!-- cratis-ai-managed: skills/cratis-arc-react-page/references/provenance.md -->
# Provenance

No TrogonStack text is adapted in this skill. Nebulit material is credited in the section below.

| Item | Source | Licence | Treatment | Where in this skill |
|---|---|---|---|---|
| Component APIs, props and hooks | Cratis Components and Arc, see *Product sources* | MIT | original | SKILL.md, references/ |
| Screenplay and Stage facts in the screen mapping | Cratis/Screenplay and Cratis/Stage documentation and source | MIT | original | references/from-screenplay.md |

## Eventmodelers Build-Kits

Source: https://github.com/Nebulit-GmbH/Eventmodelers-Build-Kits, by Martin Dilger and Nebulit GmbH
(https://nebulit.de), used with their explicit consent.

| Source skill | Used in | How |
|---|---|---|
| supabase-react `build-state-change`, `build-state-view` and build-kit composition guideline | `from-screenplay.md`, Level 1 and 2 | copied and translated: page composition through exported dialogs/components, no cross-slice internals, deletability and application-level providers |

## Adapted closely (Martin Dilger and Nebulit GmbH)

Source: https://github.com/Nebulit-GmbH/agentic-engineer at commit `07b0f30648d663cb588d7e2c7aa031af9dfc21f2`,
by Martin Dilger and Nebulit GmbH (https://nebulit.de), used with the explicit consent of Martin Dilger and Nebulit GmbH.

| Source file | Used in | How |
|---|---|---|
| `.claude/skills/html-screen/SKILL.md and eventmodeling-storyboarding-events/SKILL.md`: every displayed value has a named origin | `SKILL.md` Step 2 | idea; our own wording kept |
| `.claude/skills/build-state-change/SKILL.md`: check the implementation against its source of truth element by element | `references/from-screenplay.md` *Reconcile before you finish*, `SKILL.md` Verify | idea; our own wording kept |
| `.claude/skills/eventmodeling-storyboarding-events/SKILL.md and discover-storyboard/SKILL.md`: design the alternative states of a screen | `SKILL.md` Verify | idea; our own wording kept |
