---
agent: agent
description: "Run the discoverability check on content about to be published."
---
<!-- cratis-ai-managed: prompts/pre-publish-check.prompt.md -->

# Pre-publish check

Run the discoverability check on what is about to be published: a docs page, blog
post, landing page, README or package metadata. Follow
`.cratis/ai/rules/discoverability.md` and invoke **cratis-discoverability**.

## Identify the content

Find the files in the change, the content type of each, and the owning source (never
a synced copy). Ask only when the content type is unclear.

## Check and fix

Apply the skill's checks for each content type to the draft: opening definition,
title and description, canonical names, canonical `https://www.cratis.io/...` links,
alt text and, for sites and packages, the structured data and metadata. Make the
fixes in the owning files. Where a fix would hurt readability, keep the text and
report it. For a built site, run the skill's checker on the build output.

Report what changed, what was left alone and why, and any check that could not run.
Publishing, merging and releasing stay with a person.
