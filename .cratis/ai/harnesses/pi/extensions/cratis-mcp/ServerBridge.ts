// cratis-ai-managed: harnesses/pi/extensions/cratis-mcp/ServerBridge.ts
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { ExtensionAPI, ExtensionContext } from '@earendil-works/pi-coding-agent';
import { object } from './configuration.ts';
import type { CorpusServer } from './CorpusServer.ts';
import { startServer } from './process.ts';
import { discoverServer, mapServerResult, type ServerCatalog } from './serverProtocol.ts';
import { selectedCorpusServers } from './servers.ts';
import type { StdioConnection } from './StdioConnection.ts';

function withDeadline<T>(work: Promise<T>, milliseconds: number, id: string): Promise<T> {
    let timer: ReturnType<typeof setTimeout> | undefined;
    const expired = new Promise<never>((_resolve, reject) => {
        timer = setTimeout(() => reject(new Error(`MCP server '${id}' did not become ready within ${milliseconds} ms.`)), milliseconds);
        timer.unref();
    });
    work.catch(() => { /* The deadline already reported the outcome; the disposed connection rejects the abandoned request. */ });
    return Promise.race([work, expired]).finally(() => clearTimeout(timer));
}

/** Bridge every selected corpus server except Screenplay: one connection each, isolated failures, tools namespaced by server id. */
export interface ServerBridge {
    refresh(context: ExtensionContext, newSession: boolean): Promise<void>;
    dispose(): void;
}

export function createServerBridge(pi: ExtensionAPI, corpus: string, connect: (project: string, server: CorpusServer) => StdioConnection = startServer, deadlineMilliseconds = 15_000): ServerBridge {
    const connections = new Map<string, StdioConnection>();
    const registered = new Set<string>();
    const paused = new Set<string>();
    let selectionKey: string | undefined;

    const notify = (context: ExtensionContext, messages: string[]): void => {
        if (context.hasUI) messages.forEach(message => context.ui.notify(message, 'warning'));
    };
    const deactivate = (): void => {
        const active = pi.getActiveTools();
        active.filter(name => registered.has(name)).forEach(name => paused.add(name));
        if (active.some(name => registered.has(name))) pi.setActiveTools(active.filter(name => !registered.has(name)));
    };
    const dispose = (): void => {
        connections.forEach(connection => connection.dispose());
        connections.clear();
        deactivate();
    };
    interface Started { server: CorpusServer; connection: StdioConnection; catalog?: ServerCatalog; messages: string[] }
    /** Connect and discover only; registration happens afterwards in selection order so name claims are deterministic. */
    const start = async (context: ExtensionContext, server: CorpusServer): Promise<Started> => {
        const connection = connect(context.cwd, server);
        connections.set(server.id, connection);
        try {
            const catalog = await withDeadline(discoverServer(connection, server.id), deadlineMilliseconds, server.id);
            if (connections.get(server.id) !== connection || connection.stopped) throw new Error(`MCP server '${server.id}' session ended during discovery.`);
            return { server, connection, catalog, messages: [...catalog.messages] };
        } catch (error) {
            connection.dispose();
            if (connections.get(server.id) === connection) connections.delete(server.id);
            return { server, connection, messages: [`MCP server '${server.id}' is unavailable: ${error instanceof Error ? error.message : String(error)}`] };
        }
    };
    const register = ({ server, connection, catalog, messages }: Started, claimed: Set<string>): void => {
        const existing = new Set(pi.getAllTools().map(tool => tool.name));
        for (const tool of catalog?.tools ?? []) {
            if (claimed.has(tool.nativeName) || (existing.has(tool.nativeName) && !registered.has(tool.nativeName))) {
                messages.push(`MCP server '${server.id}' tool '${tool.name}' was skipped: '${tool.nativeName}' conflicts with another tool.`);
                continue;
            }
            claimed.add(tool.nativeName);
            pi.registerTool({
                name: tool.nativeName,
                label: `${server.id} ${tool.name}`,
                description: tool.description,
                parameters: tool.parameters,
                executionMode: 'sequential',
                async execute(_toolCallId, parameters, signal) {
                    if (connections.get(server.id) !== connection || connection.stopped || !pi.getActiveTools().includes(tool.nativeName)) {
                        throw new Error(`MCP server '${server.id}' is unavailable in this session.`);
                    }
                    if (!object(parameters)) throw new Error('MCP arguments must be an object.');
                    const result = mapServerResult(await connection.request('tools/call', { name: tool.name, arguments: parameters }, signal), server.id);
                    if (result.details.isError) throw new Error(result.content.map(part => part.type === 'text' ? part.text : '').join('\n') || `MCP server '${server.id}' reported a failed tool call.`);
                    return result;
                },
            });
        }
    };
    const refresh = async (context: ExtensionContext, newSession: boolean): Promise<void> => {
        let selection: ReturnType<typeof selectedCorpusServers>;
        try { selection = selectedCorpusServers(context.cwd, corpus); }
        catch (error) { dispose(); selectionKey = undefined; throw error; }
        const key = JSON.stringify(selection);
        if (!newSession && key === selectionKey) return;
        const previouslyActive = new Set([...pi.getActiveTools(), ...paused]);
        const oldNames = new Set(registered);
        dispose();
        selectionKey = key;
        const messages = [...selection.messages];
        const claimed = new Set<string>();
        // Servers start in parallel; a slow or hung one is abandoned at its own deadline and never delays the others.
        const outcomes = await Promise.all(selection.servers.map(server => start(context, server)));
        for (const outcome of outcomes) { register(outcome, claimed); messages.push(...outcome.messages); }
        claimed.forEach(name => registered.add(name));
        // Never re-enable a user-disabled tool. SDK allow/exclude restrictions still filter registration.
        const next = pi.getActiveTools().filter(name => !registered.has(name) || (claimed.has(name) && (!oldNames.has(name) || previouslyActive.has(name))));
        for (const name of claimed) if (oldNames.has(name) && previouslyActive.has(name)) next.push(name);
        pi.setActiveTools([...new Set(next)]);
        paused.clear();
        notify(context, messages);
    };

    return { refresh, dispose: () => { dispose(); selectionKey = undefined; } };
}
