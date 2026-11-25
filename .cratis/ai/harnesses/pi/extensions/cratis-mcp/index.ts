// cratis-ai-managed: harnesses/pi/extensions/cratis-mcp/index.ts
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { existsSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import type { ExtensionAPI, ExtensionContext } from '@earendil-works/pi-coding-agent';
import { createActivation, enabledText } from './activation.ts';
import { ConnectionFailure } from './ConnectionFailure.ts';
import { object, selectedServer } from './configuration.ts';
import type { CorpusServer } from './CorpusServer.ts';
import { startConnection, startServer, supportsNoModelStartup } from './process.ts';
import { discover, failedResult, mapResult } from './protocol.ts';
import { createServerBridge } from './ServerBridge.ts';
import type { DiscoveredTool } from './DiscoveredTool.ts';
import type { StdioConnection } from './StdioConnection.ts';

type Selection = NonNullable<ReturnType<typeof selectedServer>>;

/** One discovered tool bound to the connection it was discovered on. */
interface NativeCall { tool: DiscoveredTool; activeConnection: StdioConnection; modelRoot: string }

const extensionDirectory = dirname(fileURLToPath(import.meta.url));
const bundledCorpus = resolve(extensionDirectory, '..', '..', '..', '..');

/** Native tools go through Pi's tool_call restrictions and normal execution/result pipeline. */
export function registerBridge(pi: ExtensionAPI, corpus: string, connect = startConnection, checkVersion = supportsNoModelStartup, connectServer: (project: string, server: CorpusServer) => StdioConnection = startServer): void {
    const activation = createActivation(pi);
    const servers = createServerBridge(pi, corpus, connectServer, undefined, activation);
    const owner = 'screenplay';
    let connection: StdioConnection | undefined;
    let selectionKey: string | undefined;
    let attempted = false;
    let uncertain = false;
    let startupSupported: Promise<boolean> | undefined;
    let unavailableReported = false;
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
    const activeNames = (): string[] => pi.getActiveTools().filter(name => registered.has(name));
    const isLiveFor = (key: string): boolean => connection !== undefined && !connection.stopped && key === selectionKey;
    const assertCertain = (): void => {
        if (uncertain) throw new Error('Screenplay mutation outcome remains unknown. Explicitly reload, inspect workspace-state, and obtain authorization before any recovery.');
    };
    const refresh = async (context: ExtensionContext, newSession = false): Promise<void> => {
        if (newSession) { startupSupported = undefined; unavailableReported = false; }
        const previouslyActive = new Set([...pi.getActiveTools(), ...paused]);
        let selection: ReturnType<typeof selectedServer>;
        try { selection = currentSelection(context); }
        catch (error) { dispose(); activation.publish(owner, new Map()); throw error; }
        const key = JSON.stringify(selection);
        if (!newSession && attempted && key === selectionKey) return;
        dispose();
        attempted = true;
        selectionKey = key;
        failures.clear();
        activation.publish(owner, new Map());
        if (!selection) return;
        assertCertain();
        // Deferred by default: the tools are discovered and registered only when the model calls cratis_enable_mcp.
        if (!selection.eager) { activation.publish(owner, new Map([['screenplay', enable]])); return; }
        await activate(context, selection, previouslyActive, false);
    };
    /** On-demand start. Starts over when the selection changed since the last refresh; an unavailable CLI is an error here, not a notice. */
    const enable = async (context: ExtensionContext): Promise<string> => {
        let selection: ReturnType<typeof selectedServer>;
        try { selection = currentSelection(context); }
        catch (error) { dispose(); throw error; }
        if (!selection) throw new Error('Screenplay MCP is not selected for this project.');
        const key = JSON.stringify(selection);
        if (isLiveFor(key)) return enabledText('Screenplay MCP', activeNames(), true);
        assertCertain();
        const previouslyActive = new Set([...pi.getActiveTools(), ...paused]);
        dispose();
        attempted = true;
        selectionKey = key;
        failures.clear();
        await activate(context, selection, previouslyActive, true);
        return enabledText('Screenplay MCP', activeNames(), false);
    };
    const connectAndDiscover = async (selection: Selection): Promise<{ activeConnection: StdioConnection; tools: DiscoveredTool[] }> => {
        if (selection.composedOnly && !await (startupSupported ??= checkVersion(selection.project))) {
            throw new Error('Screenplay optional startup requires Cratis CLI 3.46.1 or later.');
        }
        const activeConnection = connect(selection.project);
        connection = activeConnection;
        const tools = await discover(activeConnection);
        if (connection !== activeConnection || activeConnection.stopped) throw new Error('Screenplay session ended during discovery.');
        return { activeConnection, tools };
    };
    const reportUnavailable = (context: ExtensionContext): void => {
        if (unavailableReported) return;
        unavailableReported = true;
        if (context.hasUI) context.ui.notify('Screenplay MCP is unavailable; install/update the Cratis CLI to 3.46.1 or later to enable it.', 'info');
    };
    const confirmationText = (parameters: Record<string, unknown>): string => {
        const argumentsText = JSON.stringify(parameters);
        if (argumentsText.length > 8192) throw new Error('Mutation confirmation arguments are too large; omit includeContent and use the proposal/operation identifiers.');
        return argumentsText;
    };
    const confirmMutation = async (call: NativeCall, parameters: Record<string, unknown>, signal: AbortSignal | undefined, toolContext: ExtensionContext): Promise<void> => {
        if (!toolContext.hasUI) throw new Error('Screenplay source mutation requires explicit user confirmation in Pi interactive/RPC mode.');
        const argumentsText = confirmationText(parameters);
        const approved = await toolContext.ui.confirm(`Screenplay ${call.tool.name}`, `Modify source under ${call.modelRoot}?\n${argumentsText}\nOnly approve the exact reviewed proposal or recovery operation.`, { timeout: 60_000, signal });
        if (!approved || signal?.aborted) throw new Error('Screenplay source mutation was not approved.');
        ensureCurrent(toolContext);
    };
    const isInvalidMutationResult = (call: NativeCall, response: unknown, toolCallId: string): boolean => call.tool.mutation && response !== undefined && !failures.has(toolCallId);
    const recordFailure = (toolCallId: string, result: ReturnType<typeof mapResult>): never => {
        if (failures.size >= 64) failures.delete(failures.keys().next().value!);
        failures.set(toolCallId, failedResult(result));
        throw new Error('Screenplay reported a failed tool call.');
    };
    const afterCallError = (call: NativeCall, response: unknown, toolCallId: string, error: unknown): never => {
        if (isInvalidMutationResult(call, response, toolCallId)) {
            uncertain = true;
            dispose();
            throw new ConnectionFailure('Screenplay mutation returned an invalid result.', true);
        }
        uncertain ||= call.activeConnection.outcomeUnknown;
        if (call.activeConnection.stopped) deactivate();
        throw error;
    };
    const forward = async (call: NativeCall, toolCallId: string, parameters: Record<string, unknown>, signal: AbortSignal | undefined) => {
        let response: unknown;
        try {
            response = await call.activeConnection.request('tools/call', { name: call.tool.name, arguments: parameters }, signal, call.tool.mutation);
            const result = mapResult(response);
            return result.details.isError ? recordFailure(toolCallId, result) : result;
        } catch (error) {
            return afterCallError(call, response, toolCallId, error);
        }
    };
    const assertCallable = (call: NativeCall, parameters: unknown, signal: AbortSignal | undefined, toolContext: ExtensionContext): Record<string, unknown> => {
        ensureCurrent(toolContext);
        if (connection !== call.activeConnection || !pi.getActiveTools().includes(call.tool.nativeName)) throw new Error('Screenplay tool is inactive in this session.');
        if (!object(parameters)) throw new Error('Screenplay arguments must be an object.');
        if (signal?.aborted) throw new Error('Screenplay call cancelled before sending.');
        return parameters;
    };
    const registerNative = (call: NativeCall): void => {
        const { tool } = call;
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
                const checked = assertCallable(call, parameters, signal, toolContext);
                if (tool.mutation) await confirmMutation(call, checked, signal, toolContext);
                return forward(call, toolCallId, checked, signal);
            },
        });
    };
    /** Never re-enable a user-disabled tool. SDK allow/exclude restrictions still filter registration. */
    const restoreActive = (tools: DiscoveredTool[], oldNames: Set<string>, previouslyActive: Set<string>): void => {
        const discovered = new Set(tools.map(tool => tool.nativeName));
        const next = pi.getActiveTools().filter(name => !registered.has(name) || (discovered.has(name) && (!oldNames.has(name) || previouslyActive.has(name))));
        for (const name of discovered) if (oldNames.has(name) && previouslyActive.has(name)) next.push(name);
        pi.setActiveTools([...new Set(next)]);
        paused.clear();
    };
    const registerAll = (tools: DiscoveredTool[], activeConnection: StdioConnection, selection: Selection, previouslyActive: Set<string>): void => {
        const existing = new Set(pi.getAllTools().map(tool => tool.name));
        if (tools.some(tool => existing.has(tool.nativeName) && !registered.has(tool.nativeName))) throw new Error('Screenplay native tool name conflicts with another extension.');
        const oldNames = new Set(registered);
        const modelRoot = selection.root ?? "the project's Screenplay model";
        tools.forEach(tool => registerNative({ tool, activeConnection, modelRoot }));
        restoreActive(tools, oldNames, previouslyActive);
    };
    const activate = async (context: ExtensionContext, selection: Selection, previouslyActive: Set<string>, explicit: boolean): Promise<void> => {
        let found: Awaited<ReturnType<typeof connectAndDiscover>>;
        try { found = await connectAndDiscover(selection); }
        catch (error) {
            dispose();
            // Application-only composition makes MCP optional. Independent profile/root errors and uncertain mutations still surface.
            const surfaces = explicit || uncertain;
            if (surfaces || !selection.composedOnly) throw error;
            reportUnavailable(context);
            return;
        }
        try { registerAll(found.tools, found.activeConnection, selection, previouslyActive); }
        catch (error) {
            if (connection === found.activeConnection) dispose();
            else found.activeConnection.dispose();
            throw error;
        }
    };

    // Screenplay and the other corpus servers fail independently; the first failure is surfaced after both ran.
    const refreshAll = async (context: ExtensionContext, newSession: boolean): Promise<void> => {
        const outcomes = await Promise.allSettled([refresh(context, newSession), servers.refresh(context, newSession)]);
        const failed = outcomes.find(outcome => outcome.status === 'rejected');
        if (failed) throw failed.reason;
    };
    pi.on('session_start', (_event, context) => refreshAll(context, true));
    pi.on('before_agent_start', async (_event, context) => { await refreshAll(context, false); });
    pi.on('session_shutdown', () => { dispose(); servers.dispose(); failures.clear(); activation.publish(owner, new Map()); });
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
