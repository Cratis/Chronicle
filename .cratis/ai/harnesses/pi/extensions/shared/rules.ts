// cratis-ai-managed: harnesses/pi/extensions/shared/rules.ts
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { existsSync, readFileSync, readdirSync } from 'node:fs';
import { join, sep } from 'node:path';
import type { AiConfiguration } from './AiConfiguration.ts';
import { corpusRoot } from './corpusRoot.ts';
import { frontmatter } from './frontmatter.ts';
import { globToRegExp, universalGlobs } from './globs.ts';
import type { ManagedRule } from './ManagedRule.ts';

function rulesRoot(cwd: string): string {
    const managedRoot = join(cwd, '.cratis', 'ai', 'rules');
    return existsSync(managedRoot) ? managedRoot : join(corpusRoot, 'rules');
}

function configuration(cwd: string): AiConfiguration | undefined {
    const path = join(cwd, '.cratis', 'ai.json');
    if (!existsSync(path)) return undefined;
    try {
        return JSON.parse(readFileSync(path, 'utf8')) as AiConfiguration;
    } catch {
        return undefined;
    }
}

/**
 * A profile-specific rule is kept only when the repository selects that profile. Repositories without
 * a `.cratis/ai.json` keep every rule, matching the `@cratis/pi` package.
 */
function matchesProfile(rule: ManagedRule, selected: AiConfiguration | undefined): boolean {
    if (!rule.profile || !selected) return true;
    const profiles = selected.profiles ?? [];
    if (rule.profile === 'application') return profiles.some(profile => profile.startsWith('cratis/application'));
    if (rule.profile === 'framework') return profiles.some(profile => profile.startsWith('cratis/engineering'));
    return true;
}

/**
 * The packaged `@cratis/pi` corpus holds every rule, so the languages and documentation a repository selected
 * decide which apply: a rule scoped to `.cs` files needs `csharp`, to `.ts` files needs `typescript`, and to
 * Markdown needs the `cratis/documentation` profile. A repository that selects no languages keeps them all.
 */
function matchesSelection(name: string, applyTo: string[], selected: AiConfiguration | undefined): boolean {
    const languages = selected?.languages ?? [];
    if (!selected || languages.length === 0) return true;
    const scope = applyTo.join(',');
    const needsCSharp = scope.includes('.cs');
    const needsTypeScript = scope.includes('.ts') || name === 'rtk.md' || name.endsWith('/rtk.md');
    const needsDocumentation = scope.includes('md');
    if (!needsCSharp && !needsTypeScript && !needsDocumentation) return true;
    return (needsCSharp && languages.includes('csharp')) ||
        (needsTypeScript && languages.includes('typescript')) ||
        (needsDocumentation && (selected.profiles ?? []).includes('cratis/documentation'));
}

/**
 * Loads every managed rule with its frontmatter interpreted, filtered to the repository's profiles. From the
 * packaged corpus, which is not resolved for the repository, the selected languages and documentation apply as
 * well. A managed `.cratis/ai/rules` is already resolved by the CLI, so that second filter is skipped there.
 */
export function managedRules(cwd: string): ManagedRule[] {
    const root = rulesRoot(cwd);
    const selected = configuration(cwd);
    const resolved = existsSync(join(cwd, '.cratis', 'ai', 'rules'));
    return readdirSync(root, { recursive: true, encoding: 'utf8' })
        .filter((entry): entry is string => entry.endsWith('.md'))
        .sort()
        .map(entry => {
            const content = readFileSync(join(root, entry), 'utf8');
            const fields = frontmatter(content);
            return {
                rule: {
                    name: entry.split(sep).join('/'),
                    content,
                    profile: fields.get('profile')?.[0],
                    globs: [...(fields.get('applyTo') ?? []), ...(fields.get('paths') ?? [])],
                } satisfies ManagedRule,
                applyTo: fields.get('applyTo') ?? [],
            };
        })
        .filter(({ rule, applyTo }) => matchesProfile(rule, selected) && (resolved || matchesSelection(rule.name, applyTo, selected)))
        .map(({ rule }) => rule);
}

/** Rules that apply to every file. These belong in the system prompt. */
export function universalRules(cwd: string): ManagedRule[] {
    return managedRules(cwd).filter(rule => rule.globs.length === 0 || rule.globs.some(glob => universalGlobs.has(glob)));
}

/** Rules whose `applyTo`/`paths` match a repository-relative path. These are delivered when that file is touched. */
export function rulesForPath(cwd: string, relativePath: string): ManagedRule[] {
    const normalized = relativePath.split(sep).join('/');
    return managedRules(cwd).filter(rule =>
        rule.globs.length > 0 &&
        !rule.globs.some(glob => universalGlobs.has(glob)) &&
        rule.globs.some(glob => globToRegExp(glob).test(normalized)));
}
