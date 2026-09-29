// cratis-ai-managed: harnesses/pi/extensions/cratis-path-guidance/touchedPaths.ts
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { statSync } from 'node:fs';
import { resolve } from 'node:path';
import { ToolName } from './ToolName.ts';

/** Upper bound on files considered from one bash command, so a wide command cannot deliver the corpus. */
const maxPathsPerCommand = 10;

export function isShellTool(toolName: string): boolean {
    return toolName === ToolName.Bash || toolName === ToolName.PowerShell;
}

export function commandOf(input: unknown): string | undefined {
    const command = (input as { command?: unknown } | undefined)?.command;
    return typeof command === 'string' ? command : undefined;
}

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

/** The file paths a tool call refers to, as the tool received them. */
export function touchedPaths(toolName: string, input: unknown, cwd: string): string[] {
    if (isShellTool(toolName)) {
        const command = commandOf(input);
        return command === undefined ? [] : pathsFromCommand(command, cwd);
    }
    if (toolName !== ToolName.Read && toolName !== ToolName.Write && toolName !== ToolName.Edit) return [];
    const candidate = (input as { path?: unknown; file_path?: unknown } | undefined);
    const value = candidate?.path ?? candidate?.file_path;
    return typeof value === 'string' && value.length > 0 ? [value] : [];
}
