// cratis-ai-managed: harnesses/pi/extensions/cratis-path-guidance/standDown.ts
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { existsSync, readFileSync } from 'node:fs';
import { join } from 'node:path';

/**
 * Whether a managed `cratis-rules` delivers path-scoped rules itself, which is every generation but the current one:
 *
 * - `6b0bb54`, `5b048c6`: `before_agent_start` concatenates every rule into the system prompt;
 * - `aca9c6b`, `59362cf`, `1937b06`, `eec744a`: universal rules in the system prompt, path-scoped rules on
 *   `tool_result`;
 * - current: universal rules only, taken from `../shared/rules.ts`, with no `tool_result` handler.
 *
 * Only a file that is recognisably the current one, importing the shared rules and having no `tool_result`
 * handler, is trusted not to deliver path rules; anything else, including a file that cannot be read, is assumed
 * to. Standing down on a guess costs allowlisted subagents their path guidance until `cratis ai update`, which
 * is cheaper than duplicating every path-scoped rule in every full session.
 */
function managedRulesDeliverPaths(cwd: string): boolean {
    const path = join(cwd, '.pi', 'extensions', 'cratis-rules', 'index.ts');
    if (!existsSync(path)) return false;
    try {
        const content = readFileSync(path, 'utf8');
        return !content.includes("'../shared/rules.ts'") || content.includes('tool_result');
    } catch {
        return true;
    }
}

/**
 * A managed installation (`.cratis/ai.manifest.json`) loads its own extensions from `.pi/extensions`. The copy
 * shipped in `@cratis/pi` stands down when that installation already delivers path guidance, so a rule or hint
 * is never delivered twice, whichever versions are mixed:
 *
 * - the managed `cratis-path-guidance` exists: it delivers rules and hints;
 * - an older managed `cratis-rules` exists, which delivers path-scoped rules itself, in the system prompt or on
 *   `tool_result`: the packaged copy would repeat every rule, so it yields until `cratis ai update` installs the
 *   managed copy;
 * - a current managed `cratis-rules` that only injects universal rules, or no managed Pi extensions at all:
 *   nothing else delivers path guidance, so the packaged copy stays active.
 *
 * The managed copy never stands down.
 */
export function standsDown(packaged: boolean, cwd: string): boolean {
    if (!packaged || !existsSync(join(cwd, '.cratis', 'ai.manifest.json'))) return false;
    return existsSync(join(cwd, '.pi', 'extensions', 'cratis-path-guidance', 'index.ts')) || managedRulesDeliverPaths(cwd);
}
