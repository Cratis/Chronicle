// cratis-ai-managed: harnesses/pi/extensions/shared/skillFrontmatter.ts
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * The `SKILL.md` frontmatter key that lists the globs a skill is hinted for. It is deliberately Cratis-specific:
 * Claude Code gives a plain `paths` key in `SKILL.md` a meaning of its own (a "conditional skill" that stays out
 * of the skill list until a matching file is touched), and `.claude/skills` exposes this corpus to it.
 */
export const skillTriggerKey = 'cratis-hint-paths';
