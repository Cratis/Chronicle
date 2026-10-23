<!-- cratis-ai-managed: skills/cratis-screenplay-automations-and-translations/references/integration-interview.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Integration interview prompts

Ask one group at a time, following up from the answer. Accept "unsure" as an open
question, not an invented answer.

1. **Sources.** "Which systems send us facts? For each: name, record types, channel
   (webhook, poll, topic, file), format, who authenticates it?" Follow-up per system:
   "Does the record carry our entity id, or do we need to remember their id against ours?"
2. **Mapping.** "For the hardest record: does it map directly to one fact of ours, need
   several facts, or need data we already hold?" Follow-up if not direct: "What do we
   look up, and what happens when the record arrives before that data exists?"
3. **Automations.** "What happens on its own after <fact>? Can it fail, wait, or need a
   person? What do people see meanwhile? Who is allowed to do it?" Follow-up: "What
   makes it stop? What if it ran twice?"
4. **Recovery.** "Which records or steps can we not accept, and who fixes them, how,
   and how does processing resume?"

Skip questions already answered by the brief or model. Missing correlation is the
most common cause of failed integrations: surface it first. Unattended work records
assumptions as labeled open decisions in `.ai-work/screenplay/<model-slug>/STATE.md`
and marks dependent slices open, never inventing mappings or actors.
