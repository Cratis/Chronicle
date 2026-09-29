// cratis-ai-managed: harnesses/pi/extensions/cratis-path-guidance/skillReads.ts
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { SkillTrigger } from './SkillTrigger.ts';
import { ToolName } from './ToolName.ts';
import { commandOf, isShellTool } from './touchedPaths.ts';

/** Shell commands that put a file's content in front of the model. `ls`, `git diff` or `git add` do not. */
const readers = new Set(['cat', 'sed', 'head', 'tail', 'less', 'bat', 'rg', 'grep']);

/** `rtk` verbs that read: `rtk read`, `rtk cat`, and `rtk grep`/`rtk rg` on a file. */
const rtkReaders = new Set(['read', ...readers]);

function escapeRegExp(text: string): string {
    return text.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

/** Whether a path names a file inside the skill's directory. */
function isInsideSkill(path: string, skill: SkillTrigger): boolean {
    if (path.includes(`${skill.baseDir}/`)) return true;
    return new RegExp(`(?:^|[/\\s'"=])skills/${escapeRegExp(skill.name)}/`).test(path.replaceAll('\\', '/'));
}

/** Whether text names the skill's `SKILL.md`, or a file under its `references/`. */
function namesSkillDocument(text: string, skill: SkillTrigger): boolean {
    const normalized = text.replaceAll('\\', '/');
    if (normalized.includes(`${skill.baseDir}/SKILL.md`) || normalized.includes(`${skill.baseDir}/references/`)) return true;
    return new RegExp(`(?:^|[/\\s'"=])skills/${escapeRegExp(skill.name)}/(?:SKILL\\.md|references/)`).test(normalized);
}

/**
 * The commands of a shell line that read a file. The line is split at `&&`, `||`, `|`, `;` and newlines, and a
 * command counts when its verb (after an optional `rtk` or `rtk proxy`) is a reader. An `echo`, a commit message
 * or `git diff` that merely mentions a path is not a read.
 */
function readerCommands(command: string): string[] {
    return command.split(/\s*(?:&&|\|\||\||;(?=\s|$)|\n)\s*/).filter(segment => {
        const words = segment.trim().split(/\s+/);
        if (words[0] !== 'rtk') return readers.has(words[0]);
        const verb = words[1] === 'proxy' ? words[2] : words[1];
        return words[1] === 'proxy' ? readers.has(verb) : rtkReaders.has(verb);
    });
}

/**
 * The skills a successful tool call read: the `read` tool on any file under `skills/<name>/`, or a shell reader
 * (`cat`, `sed`, `head`, `tail`, `less`, `bat`, `rg`, `grep`, `rtk read`) on that skill's `SKILL.md` or a file
 * under its `references/`. A model that opened either already has the skill's guidance. Listing the directory,
 * or a `git` command that names the path, gives it nothing and is not a read.
 */
export function skillsRead(toolName: string, input: unknown, skills: SkillTrigger[]): SkillTrigger[] {
    if (toolName === ToolName.Read) {
        const candidate = input as { path?: unknown; file_path?: unknown } | undefined;
        const value = candidate?.path ?? candidate?.file_path;
        return typeof value === 'string' ? skills.filter(skill => isInsideSkill(value, skill)) : [];
    }
    if (!isShellTool(toolName)) return [];
    const command = commandOf(input);
    if (command === undefined) return [];
    const commands = readerCommands(command);
    return skills.filter(skill => commands.some(segment => namesSkillDocument(segment, skill)));
}
