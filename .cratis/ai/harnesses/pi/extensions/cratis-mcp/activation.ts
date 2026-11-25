// cratis-ai-managed: harnesses/pi/extensions/cratis-mcp/activation.ts
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { ExtensionAPI, ExtensionContext, ToolDefinition } from '@earendil-works/pi-coding-agent';

/** Starts one deferred server (connect, discover, register) and returns the text the model sees. Must be idempotent once started. */
export type Enabler = (context: ExtensionContext) => Promise<string>;

export const enableToolName = 'cratis_enable_mcp';

/** The text every enabler returns on success: the tools only become callable on the next LLM step. */
export function enabledText(label: string, names: string[], alreadyEnabled: boolean): string {
    const list = names.length ? names.join(', ') : 'none';
    return alreadyEnabled
        ? `${label} is already enabled. Its tools: ${list}.`
        : `${label} is enabled. Its tools are available from the next step (not in this step's parallel tool calls): ${list}.`;
}

/**
 * Shared lifecycle for deferred MCP servers: each bridge publishes the servers it left unstarted, and one
 * `cratis_enable_mcp` tool starts any of them on demand. Concurrent enables of the same server share one start.
 */
export interface Activation {
    /** Replace every deferred server owned by `owner`; an empty map withdraws them. */
    publish(owner: string, enablers: Map<string, Enabler>): void;
    enable(id: string, context: ExtensionContext): Promise<string>;
}

export function createActivation(pi: ExtensionAPI): Activation {
    const owners = new Map<string, Map<string, Enabler>>();
    const inFlight = new Map<string, Promise<string>>();
    let registeredIds = '';

    const deferredIds = (): string[] => [...owners.values()].flatMap(enablers => [...enablers.keys()]);
    const find = (id: string): Enabler | undefined => [...owners.values()].map(enablers => enablers.get(id)).find(enabler => enabler !== undefined);

    const enable = (id: string, context: ExtensionContext): Promise<string> => {
        const running = inFlight.get(id);
        if (running) return running;
        const enabler = find(id);
        if (!enabler) return Promise.reject(new Error(`MCP server '${id}' is not deferred in this session. Deferred servers: ${deferredIds().join(', ') || 'none'}.`));
        const started = enabler(context).finally(() => inFlight.delete(id));
        inFlight.set(id, started);
        return started;
    };

    const register = (): void => {
        const ids = deferredIds();
        const key = ids.join('\0');
        if (!ids.length || key === registeredIds) return;
        registeredIds = key;
        pi.registerTool({
            name: enableToolName,
            label: 'Enable MCP server',
            description: `Start a deferred MCP server and make its tools available. Deferred servers: ${ids.join(', ')}. Call this before using any of that server's tools; they appear from the next step.`,
            parameters: { type: 'object', properties: { server: { type: 'string', enum: ids } }, required: ['server'], additionalProperties: false } as unknown as ToolDefinition['parameters'],
            executionMode: 'sequential',
            async execute(_toolCallId, parameters, _signal, _onUpdate, context) {
                const server = (parameters as { server?: unknown }).server;
                if (typeof server !== 'string') throw new Error('The server argument must be a string.');
                const text = await enable(server, context);
                return { content: [{ type: 'text', text }], details: { server } };
            },
        });
    };

    return {
        enable,
        publish(owner, enablers) {
            if (enablers.size) owners.set(owner, enablers); else owners.delete(owner);
            register();
        },
    };
}
