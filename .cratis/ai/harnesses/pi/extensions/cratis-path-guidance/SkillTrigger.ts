// cratis-ai-managed: harnesses/pi/extensions/cratis-path-guidance/SkillTrigger.ts
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/** A skill available to the session together with the path globs its `SKILL.md` declares as triggers. */
export interface SkillTrigger {
    name: string;
    /** Absolute path of the skill's `SKILL.md`. */
    filePath: string;
    /** Directory holding `SKILL.md` and the skill's references. */
    baseDir: string;
    /** The `cratis-hint-paths` frontmatter of the skill. Empty when the skill declares no trigger. */
    globs: string[];
}
