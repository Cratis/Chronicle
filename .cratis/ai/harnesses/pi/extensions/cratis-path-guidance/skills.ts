// cratis-ai-managed: harnesses/pi/extensions/cratis-path-guidance/skills.ts
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { existsSync, readFileSync, readdirSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { corpusRoot } from '../shared/corpusRoot.ts';
import { globToRegExp } from '../shared/globs.ts';
import { skillTriggerGlobs } from '../shared/skillFrontmatter.ts';
import { selectedSkillNames } from '../shared/skillSelection.ts';
import type { LoadedSkill } from './LoadedSkill.ts';
import type { SkillMatch } from './SkillMatch.ts';
import type { SkillTrigger } from './SkillTrigger.ts';

function skillAt(root: string, name: string): LoadedSkill[] {
    const filePath = join(root, name, 'SKILL.md');
    return existsSync(filePath) ? [{ name, filePath, baseDir: join(root, name) }] : [];
}

/**
 * The skills the repository selected, whether or not Pi loaded them into the session. A managed installation resolves
 * them into `.cratis/ai/skills`. Without one, the packaged corpus holds every catalog skill, so the selection
 * comes from `.cratis/ai.json` and the profile catalog; when that cannot be resolved nothing is hinted.
 */
function repositorySkills(cwd: string): LoadedSkill[] {
    const managedRoot = join(cwd, '.cratis', 'ai', 'skills');
    if (existsSync(managedRoot)) {
        return readdirSync(managedRoot, { withFileTypes: true })
            .filter(entry => entry.isDirectory())
            .flatMap(entry => skillAt(managedRoot, entry.name));
    }
    const packagedRoot = join(corpusRoot, 'skills');
    return (selectedSkillNames(cwd) ?? []).flatMap(name => skillAt(packagedRoot, name));
}

function triggerGlobs(filePath: string): string[] {
    try {
        return skillTriggerGlobs(readFileSync(filePath, 'utf8'));
    } catch {
        return [];
    }
}

/**
 * The skills available to a session with their trigger globs: the skills Pi loaded for the session
 * (`systemPromptOptions.skills`) together with the repository's selected skills, deduplicated by name with the
 * loaded one winning. The union matters because Pi's list says nothing about whether the Cratis skills are in it:
 * it can be empty (a pi-subagents agent with `skills: false`, the usual setup for cheap workers), or non-empty
 * with only personal skills (an agent with `skills: true` whose `extensions:` allowlist leaves `@cratis/pi`, and
 * so its skill paths, out). The selected skills' `SKILL.md` can still be read by path, which is what the hint asks
 * for; a corpus skill the repository did not select is never added. Skills without a `metadata.cratis-hint-paths` trigger
 * are left out.
 */
export function skillTriggers(loaded: LoadedSkill[] | undefined, cwd: string): SkillTrigger[] {
    const loadedNames = new Set((loaded ?? []).map(skill => skill.name));
    return [...(loaded ?? []), ...repositorySkills(cwd).filter(skill => !loadedNames.has(skill.name))]
        .map(skill => ({
            name: skill.name,
            filePath: skill.filePath,
            baseDir: skill.baseDir ?? dirname(skill.filePath),
            globs: triggerGlobs(skill.filePath),
        }))
        .filter(skill => skill.globs.length > 0);
}

/** The skills whose trigger matches a repository-relative path, each with the first glob that matched. */
export function skillsForPath(triggers: SkillTrigger[], relativePath: string): SkillMatch[] {
    return triggers.flatMap(skill => {
        const glob = skill.globs.find(candidate => globToRegExp(candidate).test(relativePath));
        return glob === undefined ? [] : [{ skill, glob }];
    });
}
