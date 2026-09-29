// cratis-ai-managed: harnesses/pi/extensions/cratis-path-guidance/index.ts
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { dirname, isAbsolute, relative, resolve, sep } from 'node:path';
import { fileURLToPath } from 'node:url';
import type { ExtensionAPI } from '@earendil-works/pi-coding-agent';
import type { ManagedRule } from '../shared/ManagedRule.ts';
import { rulesForPath } from '../shared/rules.ts';
import type { LoadedSkill } from './LoadedSkill.ts';
import type { SkillMatch } from './SkillMatch.ts';
import type { SkillTrigger } from './SkillTrigger.ts';
import { skillsRead } from './skillReads.ts';
import { skillsForPath, skillTriggers } from './skills.ts';
import { standsDown } from './standDown.ts';
import { ToolName } from './ToolName.ts';
import { touchedPaths } from './touchedPaths.ts';

const extensionDirectory = dirname(fileURLToPath(import.meta.url));
const isPackagedExtension = extensionDirectory.includes(`${sep}package${sep}corpus${sep}`);

function repositoryRelative(cwd: string, path: string): string | undefined {
    const relativePath = relative(cwd, resolve(cwd, path));
    return !relativePath || relativePath.startsWith('..') || isAbsolute(relativePath) ? undefined : relativePath.split(sep).join('/');
}

function displayPath(cwd: string, path: string): string {
    return repositoryRelative(cwd, path) ?? path;
}

/** One line for everything a write matched, however many skills, so overlapping triggers cost one line and not several. */
function hintLine(cwd: string, files: string[], matches: SkillMatch[]): string {
    const skills = matches.map(match => `\`${match.skill.name}\` (matched \`${match.glob}\`)`);
    const named = skills.length === 1 ? `Skill ${skills[0]} covers` : `Skills ${skills.slice(0, -1).join(', ')} and ${skills[skills.length - 1]} cover`;
    const documents = matches.map(match => displayPath(cwd, match.skill.filePath));
    const read = documents.length === 1 ? documents[0] : `${documents.slice(0, -1).join(', ')} and ${documents[documents.length - 1]}`;
    return `[cratis-path-guidance] ${named} ${files.join(', ')}; read ${read} before continuing.`;
}

/**
 * pi-subagents writes `# Preloaded Skill: <name>` for every name in `skills:` and then the skill's text, or a
 * placeholder when it could not load it: `(Skill "<name>" not found in ...)` or `(Skill "<name>" skipped: ...)`.
 * It cannot load a skill from a symlinked skills root, which is how a managed installation exposes
 * `.pi/skills` and `.agents/skills`. Only a header followed by real content puts the skill in context.
 */
const placeholderBody = /^\(Skill "[^"]*" (?:not found|skipped)\b/;

/** The skills pi-subagents preloaded into the system prompt (`skills: a, b`) whose full text is already in context. */
function preloadedSkillNames(systemPrompt: unknown): Set<string> {
    const names = new Set<string>();
    if (typeof systemPrompt !== 'string') return names;
    for (const match of systemPrompt.matchAll(/^# Preloaded Skill: (\S+)[ \t]*\r?\n/gm)) {
        const body = systemPrompt.slice(match.index + match[0].length).split(/^# Preloaded Skill: /m)[0].trim();
        if (body.length > 0 && !placeholderBody.test(body)) names.add(match[1]);
    }
    return names;
}

/** The skills a `/skill:name` command expanded into the user message, which arrive without a `read` call. */
function expandedSkillNames(prompt: unknown): string[] {
    return typeof prompt === 'string' ? [...prompt.matchAll(/<skill name="([^"]+)"/g)].map(match => match[1]) : [];
}

/**
 * Delivers the corpus guidance that belongs to a file at the moment the file is touched, so it works in every
 * Pi process, including a subagent, without the universal rules `cratis-rules` puts in every system prompt.
 *
 * Path-scoped rules are attached to the tool result the first time a matching file is touched in the session,
 * exactly as they were when `cratis-rules` delivered them. On a successful `write` or `edit`, one advisory line
 * names every skill whose `metadata.cratis-hint-paths` frontmatter matches the file and where its `SKILL.md` is. A skill
 * is not hinted when it is already in context: read in the session, preloaded by pi-subagents
 * (`# Preloaded Skill: <name>` followed by its text in the system prompt), or expanded by `/skill:<name>`. Hints are advisory only:
 * nothing is blocked and the system prompt is never touched.
 *
 * Delivery happens on `tool_result`, so guidance arrives after the call that first touched the file.
 * `ToolCallEventResult` carries only `block`/`reason`/`terminate`, so there is no supported way to add
 * context before a tool runs. In practice a file is read before it is edited, and reads through both the read
 * tool and bash are covered; a file created blind by `write` is the residual case, and it receives the
 * guidance with that result.
 *
 * Delivered guidance lives in the conversation rather than the system prompt, so anything that rewrites or
 * replaces the conversation can remove it. The delivery record is therefore reset whenever that happens, and the
 * guidance is delivered again the next time one of its files is touched.
 */
export default function (pi: ExtensionAPI): void {
    if (standsDown(isPackagedExtension, process.cwd())) return;

    const deliveredRules = new Set<string>();
    const hintedSkills = new Set<string>();
    const readSkills = new Set<string>();
    // Preloaded skills live in the system prompt, which compaction leaves alone, so this is replaced at each
    // agent start rather than reset with the conversation.
    let preloadedSkills = new Set<string>();
    let loadedSkills: LoadedSkill[] | undefined;
    let loadedKey: string | undefined;
    let triggers: SkillTrigger[] | undefined;

    const availableTriggers = (cwd: string): SkillTrigger[] => triggers ??= skillTriggers(loadedSkills, cwd);

    // Compaction summarizes the conversation, which can drop an injected rule or hint while leaving the
    // delivery record claiming it is present; a switch replaces the conversation outright.
    const reset = () => {
        deliveredRules.clear();
        hintedSkills.clear();
        readSkills.clear();
        triggers = undefined;
    };
    pi.on('session_start', reset);
    pi.on('session_compact', reset);
    pi.on('session_before_switch', reset);

    // Only observes which skills are in context for this session; the system prompt is never changed.
    pi.on('before_agent_start', event => {
        const observed = event as { systemPrompt?: unknown; prompt?: unknown; systemPromptOptions?: { skills?: LoadedSkill[] } };
        preloadedSkills = preloadedSkillNames(observed.systemPrompt);
        expandedSkillNames(observed.prompt).forEach(name => readSkills.add(name));
        const skills = observed.systemPromptOptions?.skills;
        const key = skills?.map(skill => `${skill.name}\t${skill.filePath}`).join('\n');
        if (key !== loadedKey) triggers = undefined;
        loadedSkills = skills;
        loadedKey = key;
        return undefined;
    });

    pi.on('tool_result', (event, context) => {
        if (event.isError) return;
        const cwd = context.cwd;
        const toolName: string = event.toolName;
        const input = event.input;

        const reads = skillsRead(toolName, input, availableTriggers(cwd));
        reads.forEach(skill => readSkills.add(skill.name));

        const pendingRules: ManagedRule[] = [];
        const matchedFiles: string[] = [];
        const hintedFiles: string[] = [];
        const pendingMatches: SkillMatch[] = [];
        for (const path of touchedPaths(toolName, input, cwd)) {
            const relativePath = repositoryRelative(cwd, path);
            if (!relativePath) continue;
            for (const rule of rulesForPath(cwd, relativePath)) {
                if (deliveredRules.has(rule.name) || pendingRules.some(candidate => candidate.name === rule.name)) continue;
                pendingRules.push(rule);
                if (!matchedFiles.includes(relativePath)) matchedFiles.push(relativePath);
            }
            if (toolName !== ToolName.Write && toolName !== ToolName.Edit) continue;
            for (const match of skillsForPath(availableTriggers(cwd), relativePath)) {
                const name = match.skill.name;
                if (hintedSkills.has(name) || readSkills.has(name) || preloadedSkills.has(name)) continue;
                hintedSkills.add(name);
                pendingMatches.push(match);
                if (!hintedFiles.includes(relativePath)) hintedFiles.push(relativePath);
            }
        }
        if (pendingRules.length === 0 && pendingMatches.length === 0) return;
        pendingRules.forEach(rule => deliveredRules.add(rule.name));

        const existing = Array.isArray(event.content) ? event.content : [];
        const text = [
            pendingRules.length === 0
                ? undefined
                : `\n\n[cratis-rules] Rules that apply to ${matchedFiles.join(', ')}:\n\n${pendingRules.map(rule => rule.content).join('\n\n')}`,
            pendingMatches.length === 0 ? undefined : `\n\n${hintLine(cwd, hintedFiles, pendingMatches)}`,
        ].filter(part => part !== undefined).join('');
        return { content: [...existing, { type: 'text', text }] };
    });
}
