// cratis-ai-managed: harnesses/pi/extensions/cratis-mcp/index.ts
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { existsSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import type { ExtensionAPI, ExtensionContext } from '@earendil-works/pi-coding-agent';
import { ConnectionFailure } from './ConnectionFailure.ts';
import { object, selectedServer } from './configuration.ts';
import { startConnection } from './process.ts';
import { discover, failedResult, mapResult } from './protocol.ts';
import type { StdioConnection } from './StdioConnection.ts';

const extensionDirectory = dirname(fileURLToPath(import.meta.url));
const bundledCorpus = resolve(extensionDirectory, '..', '..', '..', '..');

/** Native tools go through Pi's tool_call restrictions and normal execution/result pipeline. */
export function registerBridge(pi: ExtensionAPI, corpus: string, connect = startConnection): void {
    let connection: StdioConnection | undefined;
    let selectionKey: string | undefined;
    let attempted = false;
    let uncertain = false;
    const registered = new Set<string>();
    const paused = new Set<string>();
    const failures = new Map<string, ReturnType<typeof failedResult>>();

    const deactivate = (): void => {
        const active = pi.getActiveTools();
        active.filter(name => registered.has(name)).forEach(name => paused.add(name));
        if (active.some(name => registered.has(name))) pi.setActiveTools(active.filter(name => !registered.has(name)));
    };
    const dispose = (): void => {
        connection?.dispose();
        uncertain ||= connection?.outcomeUnknown === true;
        connection = undefined;
        deactivate();
    };
    const currentSelection = (context: ExtensionContext) => selectedServer(context.cwd, corpus);
    const ensureCurrent = (context: ExtensionContext): void => {
        let key: string | undefined;
        try { key = JSON.stringify(currentSelection(context)); }
        catch (error) { dispose(); throw error; }
        if (!key || key !== selectionKey) {
            dispose();
            throw new Error('Screenplay profile/root changed. Start a new turn to rediscover tools; never reuse old workspace handles.');
        }
        if (!connection || connection.stopped) throw new Error('Screenplay connection is unavailable. No request was retried; explicitly reload after inspecting any uncertain mutation.');
    };
    const refresh = async (context: ExtensionContext, newSession = false): Promise<void> => {
        const previouslyActive = new Set([...pi.getActiveTools(), ...paused]);
        let selection: ReturnType<typeof selectedServer>;
        try { selection = currentSelection(context); }
        catch (error) { dispose(); throw error; }
        const key = JSON.stringify(selection);
        if (!newSession && attempted && key === selectionKey) return;
        dispose();
        attempted = true;
        selectionKey = key;
        failures.clear();
        if (!selection) return;
        if (uncertain) throw new Error('Screenplay mutation outcome remains unknown. Explicitly reload, inspect workspace-state, and obtain authorization before any recovery.');
        const modelRoot = selection.root;
        const activeConnection = connect(selection.project);
        connection = activeConnection;
        try {
            const tools = await discover(activeConnection);
            if (connection !== activeConnection || activeConnection.stopped) throw new Error('Screenplay session ended during discovery.');
            const existing = new Set(pi.getAllTools().map(tool => tool.name));
            if (tools.some(tool => existing.has(tool.nativeName) && !registered.has(tool.nativeName))) throw new Error('Screenplay native tool name conflicts with another extension.');
            const oldNames = new Set(registered);
            for (const tool of tools) {
                registered.add(tool.nativeName);
                pi.registerTool({
                    name: tool.nativeName,
                    label: `Screenplay ${tool.name}`,
                    description: tool.description,
                    parameters: tool.parameters,
                    executionMode: 'sequential',
                    promptGuidelines: [
                        'Use the Screenplay model-authoring skill. Read/propose/review before applying; only a direct user request authorizes apply or recovery.',
                        'Use bounded pages. On an unknown mutation outcome, never retry or recover automatically; inspect workspace-state first.',
                    ],
                    async execute(toolCallId, parameters, signal, _onUpdate, toolContext) {
                        ensureCurrent(toolContext);
                        if (connection !== activeConnection || !pi.getActiveTools().includes(tool.nativeName)) throw new Error('Screenplay tool is inactive in this session.');
                        if (!object(parameters)) throw new Error('Screenplay arguments must be an object.');
                        if (signal?.aborted) throw new Error('Screenplay call cancelled before sending.');
                        if (tool.mutation) {
                            if (!toolContext.hasUI) throw new Error('Screenplay source mutation requires explicit user confirmation in Pi interactive/RPC mode.');
                            const argumentsText = JSON.stringify(parameters);
                            if (argumentsText.length > 8192) throw new Error('Mutation confirmation arguments are too large; omit includeContent and use the proposal/operation identifiers.');
                            const approved = await toolContext.ui.confirm(`Screenplay ${tool.name}`, `Modify source under ${modelRoot}?\n${argumentsText}\nOnly approve the exact reviewed proposal or recovery operation.`, { timeout: 60_000, signal });
                            if (!approved || signal?.aborted) throw new Error('Screenplay source mutation was not approved.');
                            ensureCurrent(toolContext);
                        }
                        let response: unknown;
                        try {
                            response = await activeConnection.request('tools/call', { name: tool.name, arguments: parameters }, signal, tool.mutation);
                            const result = mapResult(response);
                            if (result.details.isError) {
                                if (failures.size >= 64) failures.delete(failures.keys().next().value!);
                                failures.set(toolCallId, failedResult(result));
                                throw new Error('Screenplay reported a failed tool call.');
                            }
                            return result;
                        } catch (error) {
                            if (tool.mutation && response !== undefined && !failures.has(toolCallId)) {
                                uncertain = true;
                                dispose();
                                throw new ConnectionFailure('Screenplay mutation returned an invalid result.', true);
                            }
                            uncertain ||= activeConnection.outcomeUnknown;
                            if (activeConnection.stopped) deactivate();
                            throw error;
                        }
                    },
                });
            }
            // Never re-enable a user-disabled tool. SDK allow/exclude restrictions still filter registration.
            const discovered = new Set(tools.map(tool => tool.nativeName));
            const next = pi.getActiveTools().filter(name => !registered.has(name) || (discovered.has(name) && (!oldNames.has(name) || previouslyActive.has(name))));
            for (const name of discovered) if (oldNames.has(name) && previouslyActive.has(name)) next.push(name);
            pi.setActiveTools([...new Set(next)]);
            paused.clear();
        } catch (error) {
            if (connection === activeConnection) dispose();
            else activeConnection.dispose();
            throw error;
        }
    };

    pi.on('session_start', (_event, context) => refresh(context, true));
    pi.on('before_agent_start', async (_event, context) => { await refresh(context); });
    pi.on('session_shutdown', () => { dispose(); failures.clear(); });
    pi.on('tool_result', event => {
        if (!registered.has(event.toolName)) return;
        const failure = failures.get(event.toolCallId);
        failures.delete(event.toolCallId);
        return failure;
    });
}

export default function (pi: ExtensionAPI): void {
    // A managed project already loads its own bridge. Do not run a second subprocess from @cratis/pi.
    const managedEntry = join(process.cwd(), '.pi', 'extensions', 'cratis-mcp', 'index.ts');
    if (bundledCorpus.endsWith(join('package', 'corpus')) && existsSync(join(process.cwd(), '.cratis', 'ai.manifest.json')) && existsSync(managedEntry)) return;
    const corpus = existsSync(join(bundledCorpus, 'mcp-servers.json')) ? bundledCorpus : join(process.cwd(), '.cratis', 'ai');
    registerBridge(pi, corpus);
}
