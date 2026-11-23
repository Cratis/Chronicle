// cratis-ai-managed: harnesses/pi/extensions/cratis-rules/index.ts
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { BeforeAgentStartEvent, ExtensionAPI } from '@earendil-works/pi-coding-agent';
import { universalRules } from '../shared/rules.ts';

function normalizedContent(content: string): string {
    return content.replace(/^\uFEFF/, '').replace(/\r\n?/g, '\n').trim();
}

function generalAlreadyIncluded(content: string, event: BeforeAgentStartEvent): boolean {
    try {
        const contextFiles = event.systemPromptOptions?.contextFiles;
        if (!Array.isArray(contextFiles) || typeof event.systemPrompt !== 'string') return false;
        const normalized = normalizedContent(content);
        if (!normalized || !normalizedContent(event.systemPrompt).includes(normalized)) return false;
        return contextFiles.some(file => typeof file?.content === 'string' && normalizedContent(file.content) === normalized);
    } catch {
        // Earlier handlers can mutate shared construction options; invalid evidence must retain guidance.
        return false;
    }
}

/**
 * Puts the universal rules in the system prompt, giving Pi the same rule semantics as the other harnesses.
 * Rules scoped by `applyTo`, `paths` or `profile` are not concatenated into every turn; the
 * `cratis-path-guidance` extension attaches path-scoped rules the first time a matching file is touched.
 *
 * The universal rules are a large, fixed cost on every turn (about 26k tokens), which is why a session that
 * only wants path guidance and the hooks should load `cratis-path-guidance` and `cratis-hooks` instead.
 */
export default function (pi: ExtensionAPI): void {
    pi.on('before_agent_start', (event, context) => {
        const contents = universalRules(context.cwd)
            .filter(rule => rule.name !== 'general.md' || !generalAlreadyIncluded(rule.content, event))
            .map(rule => rule.content)
            .join('\n\n');
        return { systemPrompt: `${event.systemPrompt}\n\n${contents}` };
    });
}
