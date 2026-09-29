// cratis-ai-managed: harnesses/pi/extensions/shared/skillFrontmatter.ts
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { frontmatterMap } from './frontmatter.ts';

// The key, under the `metadata` map of `SKILL.md` frontmatter, whose value lists the globs a skill is hinted for.
// Agent Skills allows only `name`, `description`, `license`, `compatibility`, `metadata` and `allowed-tools` at the
// top level, and `metadata` maps strings to strings, so the value is one string of whitespace-separated globs:
//
//     metadata:
//       cratis-hint-paths: "**/for_*/**/*.cs **/Documentation/**/*.{md,mdx}"
//
// It is deliberately Cratis-specific: Claude Code gives a plain `paths` key in `SKILL.md` a meaning of its own (a
// "conditional skill" that stays out of the skill list until a matching file is touched), and `.claude/skills`
// exposes this corpus to it.
export const skillTriggerKey = 'cratis-hint-paths';

/** The raw `metadata.cratis-hint-paths` string of a `SKILL.md`, or `undefined` when the skill declares none. */
export function skillTriggerValue(content: string): string | undefined {
    return frontmatterMap(content, 'metadata')?.get(skillTriggerKey);
}

/** Splits a trigger value into its globs. Globs contain no whitespace, and brace sets keep their commas. */
export function splitSkillTriggerGlobs(value: string): string[] {
    return value.split(/\s+/).filter(Boolean);
}

/** The trigger globs a `SKILL.md` declares under `metadata.cratis-hint-paths`; empty when it declares none. */
export function skillTriggerGlobs(content: string): string[] {
    return splitSkillTriggerGlobs(skillTriggerValue(content) ?? '');
}
