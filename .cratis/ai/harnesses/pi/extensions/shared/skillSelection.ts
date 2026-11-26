// cratis-ai-managed: harnesses/pi/extensions/shared/skillSelection.ts
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { existsSync, readFileSync } from 'node:fs';
import { join } from 'node:path';
import type { AiConfiguration } from './AiConfiguration.ts';
import { corpusRoot } from './corpusRoot.ts';

interface Profile {
    id: string;
    composes?: string[];
    availableTargets?: string[];
    languages?: string[];
}

interface Catalog {
    publicProfiles: Profile[];
    engineeringProfiles: Profile[];
}

/** The catalog beside the repository corpus (`.cratis/ai`), or beside `package/corpus` in the published package. */
function catalog(): Profile[] | undefined {
    const path = [join(corpusRoot, 'profile-catalog.json'), join(corpusRoot, '..', 'profile-catalog.json')].find(existsSync);
    if (path === undefined) return undefined;
    const parsed = JSON.parse(readFileSync(path, 'utf8')) as Catalog;
    return [...parsed.publicProfiles, ...parsed.engineeringProfiles];
}

function supportsLanguage(profile: Profile, languages: Set<string>): boolean {
    if (!languages.size || !profile.languages?.length) return true;
    return profile.languages.some(language => language === 'language-agnostic' || languages.has(language));
}

function profileForId(profiles: Profile[], selection: { id: string; allowUnknown: boolean }): Profile {
    const profile = profiles.find(candidate => candidate.id === selection.id);
    if (profile) return profile;
    if (!selection.allowUnknown) throw new Error(`Unknown Cratis AI profile '${selection.id}'.`);
    return { id: selection.id };
}

function resolveProfiles(configuration: AiConfiguration, profiles: Profile[], allowUnknown: boolean): Set<string> {
    const languages = new Set(configuration.languages ?? []);
    const selected = new Set<string>();
    const select = (id: string, explicitSelection: boolean): void => {
        const profile = profileForId(profiles, { id, allowUnknown });
        if (!explicitSelection && !supportsLanguage(profile, languages)) return;
        if (selected.has(id)) return;
        selected.add(id);
        profile.composes?.forEach(child => select(child, false));
    };
    configuration.profiles?.forEach(profile => select(profile, true));
    return selected;
}

/** Effective rule profiles, retaining explicit custom profile IDs when they are not in the catalog. */
export function selectedProfileIds(configuration: AiConfiguration): Set<string> {
    return resolveProfiles(configuration, catalog() ?? [], true);
}

/**
 * The skills the packaged `@cratis/pi` makes available for a repository, resolved from `.cratis/ai.json` and the
 * profile catalog exactly as `selectedSkillPaths` in `@cratis/pi` does (a repository without `ai.json` gets every
 * catalog skill, because that is what the package loads for it). Undefined when the selection cannot be resolved,
 * so a caller hints nothing rather than guessing.
 *
 * This mirrors `Source/Pi.Plugin/src/index.ts`, which cannot import from the corpus in both the repository and
 * the published layout; a specification keeps the two in step.
 */
export function selectedSkillNames(cwd: string): string[] | undefined {
    try {
        const profiles = catalog();
        if (profiles === undefined) return undefined;
        const configurationPath = join(cwd, '.cratis', 'ai.json');
        if (!existsSync(configurationPath)) {
            return [...new Set(profiles.flatMap(profile => profile.availableTargets ?? []))].sort();
        }
        const configuration = JSON.parse(readFileSync(configurationPath, 'utf8')) as AiConfiguration;
        const selected = resolveProfiles(configuration, profiles, false);
        return [...new Set(profiles.filter(profile => selected.has(profile.id)).flatMap(profile => profile.availableTargets ?? []))].sort();
    } catch {
        return undefined;
    }
}
