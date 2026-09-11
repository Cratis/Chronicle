// cratis-ai-managed: harnesses/pi/extensions/cratis-rules/index.ts
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { existsSync, readFileSync, readdirSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import type { ExtensionAPI } from '@earendil-works/pi-coding-agent';

const corpusRoot = resolve(dirname(fileURLToPath(import.meta.url)), '..', '..', '..', '..');

/** Loads every rule from the managed corpus, including general guidance when AGENTS.md is project-owned. */
export function managedRules(cwd: string): string {
    const managedRoot = join(cwd, '.cratis', 'ai', 'rules');
    const rulesRoot = existsSync(managedRoot) ? managedRoot : join(corpusRoot, 'rules');
    return readdirSync(rulesRoot, { recursive: true, encoding: 'utf8' })
        .filter((entry): entry is string => entry.endsWith('.md'))
        .sort()
        .map(entry => readFileSync(join(rulesRoot, entry), 'utf8'))
        .join('\n\n');
}

/** Adds every managed Cratis rule to Pi without requiring the @cratis/pi package. */
export default function (pi: ExtensionAPI): void {
    pi.on('before_agent_start', (event, context) => ({
        systemPrompt: `${event.systemPrompt}\n\n${managedRules(context.cwd)}`,
    }));
}
