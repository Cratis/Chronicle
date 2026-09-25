// cratis-ai-managed: harnesses/pi/extensions/cratis-rules/index.ts
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { existsSync, readFileSync, readdirSync, statSync } from 'node:fs';
import { dirname, join, relative, resolve, sep } from 'node:path';
import { fileURLToPath } from 'node:url';
import type { ExtensionAPI } from '@earendil-works/pi-coding-agent';

const corpusRoot = resolve(dirname(fileURLToPath(import.meta.url)), '..', '..', '..', '..');

type AiConfiguration = { profiles?: string[] };

export interface ManagedRule {
    /** Path relative to the rules root, e.g. `code-quality-csharp.md` or `project/running-the-local-stack.md`. */
    name: string;
    /** Full file content, frontmatter included, exactly as the other harnesses receive it. */
    content: string;
    /** `application`, `framework`, or undefined when the rule is not profile-specific. */
    profile?: string;
    /** Globs from `applyTo` and `paths`. Empty means the rule applies to every file. */
    globs: string[];
}

const universalGlobs = new Set(['**', '**/*', '*']);

function unquote(value: string): string {
    return value.trim().replace(/^["']|["']$/g, '');
}

/**
 * Reads the YAML-ish frontmatter the corpus uses: scalar `key: value` lines and `key:` followed by
 * `  - item` lines. Anything richer is not used by the rules and is deliberately not supported.
 */
function frontmatter(content: string): Map<string, string[]> {
    const fields = new Map<string, string[]>();
    if (!content.startsWith('---\n')) return fields;
    const end = content.indexOf('\n---\n', 4);
    if (end < 0) return fields;
    let current: string | undefined;
    for (const line of content.slice(4, end).split('\n')) {
        const item = /^\s+-\s+(.*)$/.exec(line);
        if (item && current) {
            fields.get(current)!.push(unquote(item[1]));
            continue;
        }
        const scalar = /^([A-Za-z][\w-]*):\s*(.*)$/.exec(line);
        if (!scalar) continue;
        current = scalar[1];
        const value = unquote(scalar[2]);
        fields.set(current, value ? value.split(',').map(unquote).filter(Boolean) : []);
    }
    return fields;
}

function escapeRegExp(text: string): string {
    return text.replace(/[.+^$()|[\]\\]/g, '\\$&');
}

/** Converts the glob dialect used by `applyTo` (`**`, `*`, `?`, `{a,b}`) into an anchored RegExp. */
export function globToRegExp(glob: string): RegExp {
    let pattern = '';
    for (let index = 0; index < glob.length; index++) {
        const character = glob[index];
        if (character === '*') {
            if (glob[index + 1] === '*') {
                if (glob[index + 2] === '/') {
                    pattern += '(?:.*/)?';
                    index += 2;
                } else {
                    pattern += '.*';
                    index += 1;
                }
            } else {
                pattern += '[^/]*';
            }
        } else if (character === '?') {
            pattern += '[^/]';
        } else if (character === '{') {
            const close = glob.indexOf('}', index);
            if (close > index) {
                pattern += `(?:${glob.slice(index + 1, close).split(',').map(part => escapeRegExp(part.trim())).join('|')})`;
                index = close;
            } else {
                pattern += '\\{';
            }
        } else {
            pattern += escapeRegExp(character);
        }
    }
    return new RegExp(`^${pattern}$`);
}

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

/** Loads every managed rule with its frontmatter interpreted, filtered to the repository's profiles. */
export function managedRules(cwd: string): ManagedRule[] {
    const root = rulesRoot(cwd);
    const selected = configuration(cwd);
    return readdirSync(root, { recursive: true, encoding: 'utf8' })
        .filter((entry): entry is string => entry.endsWith('.md'))
        .sort()
        .map(entry => {
            const content = readFileSync(join(root, entry), 'utf8');
            const fields = frontmatter(content);
            return {
                name: entry.split(sep).join('/'),
                content,
                profile: fields.get('profile')?.[0],
                globs: [...(fields.get('applyTo') ?? []), ...(fields.get('paths') ?? [])],
            } satisfies ManagedRule;
        })
        .filter(rule => matchesProfile(rule, selected));
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

/** Upper bound on files considered from one bash command, so a wide command cannot deliver the corpus. */
const maxPathsPerCommand = 10;

/**
 * Extracts file paths a bash command refers to. `rtk.md` tells agents to run `rtk read`, `rtk grep`
 * and `rtk find` from the terminal for bulk reads, so file access frequently arrives as a bash
 * command rather than the read tool; without this, a session that follows that rule would never
 * receive a path-scoped rule. A token counts only when it resolves to a file that exists inside the
 * working directory, which keeps a filename mentioned inside a commit message from matching.
 */
function pathsFromCommand(command: string, cwd: string): string[] {
    const found: string[] = [];
    for (const raw of command.split(/[\s;|&()<>]+/)) {
        if (found.length >= maxPathsPerCommand) break;
        const token = raw.replace(/^['"]+|['"]+$/g, '').replace(/[,:]+$/, '');
        if (!token || token.startsWith('-') || !/[./]/.test(token)) continue;
        try {
            if (statSync(resolve(cwd, token)).isFile()) found.push(token);
        } catch {
            /* not a path we can see; ignore */
        }
    }
    return found;
}

function touchedPaths(toolName: string, input: unknown, cwd: string): string[] {
    if (toolName === 'bash' || toolName === 'powershell') {
        const command = (input as { command?: unknown } | undefined)?.command;
        return typeof command === 'string' ? pathsFromCommand(command, cwd) : [];
    }
    if (toolName !== 'read' && toolName !== 'write' && toolName !== 'edit') return [];
    const candidate = (input as { path?: unknown; file_path?: unknown } | undefined);
    const value = candidate?.path ?? candidate?.file_path;
    return typeof value === 'string' && value.length > 0 ? [value] : [];
}

/**
 * Gives Pi the same rule semantics as the other harnesses: universal rules in the system prompt, and
 * path-scoped rules attached the first time a matching file is touched in the session. Without this,
 * every rule was concatenated into every turn regardless of `applyTo`, `paths`, or `profile`.
 *
 * Delivery happens on `tool_result`, so a rule arrives after the call that first touched its file.
 * `ToolCallEventResult` carries only `block`/`reason`/`terminate`, so there is no supported way to add
 * context before a tool runs. In practice a file is read before it is edited, and reads through both
 * the read tool and bash are covered, so the rule is present before the edit; a file created blind by
 * `write` is the residual case, and it receives the rule with that result.
 *
 * A delivered rule lives in the conversation rather than the system prompt, so anything that rewrites
 * or replaces the conversation can remove it. The delivery record is therefore reset whenever that
 * happens, and the rule is delivered again the next time one of its files is touched.
 */
export default function (pi: ExtensionAPI): void {
    const delivered = new Set<string>();

    // Compaction summarizes the conversation, which can drop an injected rule while leaving the
    // delivery record claiming it is present; a switch replaces the conversation outright. Without
    // this reset a long session would silently lose its scoped rules and never see them again.
    const reset = () => {
        delivered.clear();
    };
    pi.on('session_start', reset);
    pi.on('session_compact', reset);
    pi.on('session_before_switch', reset);

    pi.on('before_agent_start', (event, context) => ({
        systemPrompt: `${event.systemPrompt}\n\n${universalRules(context.cwd).map(rule => rule.content).join('\n\n')}`,
    }));

    pi.on('tool_result', (event, context) => {
        if (event.isError) return;
        const paths = touchedPaths(event.toolName, (event as { input?: unknown }).input, context.cwd);
        if (paths.length === 0) return;
        const pending: ManagedRule[] = [];
        const matched: string[] = [];
        for (const path of paths) {
            const relativePath = relative(context.cwd, resolve(context.cwd, path));
            if (!relativePath || relativePath.startsWith('..')) continue;
            for (const rule of rulesForPath(context.cwd, relativePath)) {
                if (delivered.has(rule.name) || pending.some(candidate => candidate.name === rule.name)) continue;
                pending.push(rule);
                if (!matched.includes(relativePath)) matched.push(relativePath);
            }
        }
        if (pending.length === 0) return;
        pending.forEach(rule => delivered.add(rule.name));
        const existing = Array.isArray(event.content) ? event.content : [];
        return {
            content: [
                ...existing,
                {
                    type: 'text',
                    text: `\n\n[cratis-rules] Rules that apply to ${matched.map(path => path.split(sep).join('/')).join(', ')}:\n\n${pending.map(rule => rule.content).join('\n\n')}`,
                },
            ],
        };
    });
}
