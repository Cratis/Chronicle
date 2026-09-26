// cratis-ai-managed: harnesses/pi/extensions/cratis-mcp/protocol.ts
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { TSchema } from 'typebox';
import { object } from './configuration.ts';
import type { DiscoveredTool } from './DiscoveredTool.ts';
import type { StdioConnection } from './StdioConnection.ts';

/** Negotiate the server's implemented protocol before exposing any discovered tools. */
export async function discover(connection: StdioConnection): Promise<DiscoveredTool[]> {
    const initialized = await connection.request('initialize', {
        protocolVersion: '2025-06-18', capabilities: {}, clientInfo: { name: 'cratis.pi', version: '1.0.0' },
    });
    if (!object(initialized) || initialized.protocolVersion !== '2025-06-18' || !object(initialized.serverInfo) ||
        initialized.serverInfo.name !== 'cratis.screenplay' || !object(initialized.capabilities) || !object(initialized.capabilities.tools) ||
        initialized.capabilities.tools.listChanged === true) throw new Error('Unsupported Screenplay MCP server capabilities or protocol version.');
    connection.notify('notifications/initialized');
    const listed = await connection.request('tools/list', {});
    if (!object(listed) || !Array.isArray(listed.tools) || !listed.tools.length || listed.tools.length > 128 || listed.nextCursor !== undefined) {
        throw new Error('Expected a nonempty, unpaginated Screenplay tool catalog (at most 128 tools).');
    }
    const names = new Set<string>();
    return listed.tools.map((tool: unknown) => {
        if (!object(tool) || typeof tool.name !== 'string' || !/^[a-z][a-z0-9-]{0,47}$/.test(tool.name) ||
            typeof tool.description !== 'string' || tool.description.length > 8192 || !object(tool.inputSchema) || tool.inputSchema.type !== 'object') {
            throw new Error('Invalid Screenplay tool name, description, or input schema.');
        }
        const nativeName = `screenplay_${tool.name.replaceAll('-', '_')}`;
        if (names.has(nativeName)) throw new Error('Duplicate Screenplay tool name.');
        names.add(nativeName);
        // Hints are not authority. Only the pinned apply/recover operations may mutate source.
        const mutation = tool.name === 'apply' || tool.name === 'recover-workspace';
        if (!mutation && (!object(tool.annotations) || tool.annotations.readOnlyHint !== true || tool.annotations.destructiveHint === true)) {
            throw new Error(`Unrecognized source-mutating Screenplay tool '${tool.name}'; update the reviewed bridge.`);
        }
        return { name: tool.name, nativeName, description: tool.description, parameters: tool.inputSchema as TSchema, mutation };
    });
}

/** Keep structured data once in details and never present server failures as success. */
export function mapResult(result: unknown) {
    if (!object(result) || !Array.isArray(result.content) || (result.isError !== undefined && typeof result.isError !== 'boolean') ||
        (result.structuredContent !== undefined && !object(result.structuredContent))) throw new Error('Malformed Screenplay tool result.');
    const texts = result.content.map((part: unknown) => {
        if (!object(part) || part.type !== 'text' || typeof part.text !== 'string') throw new Error('Unsupported Screenplay result content; expected text.');
        return part.text;
    });
    const fullText = texts.join('\n');
    const maximumCharacters = 32 * 1024;
    const truncated = fullText.length > maximumCharacters;
    const text = truncated
        ? `${fullText.slice(0, maximumCharacters)}\n[Screenplay display truncated. Request a smaller page or narrower view; full structured data is retained in tool details.]`
        : fullText;
    return {
        content: [{ type: 'text' as const, text }],
        details: {
            structuredContent: result.structuredContent,
            isError: result.isError === true,
            truncated,
            ...(truncated && result.structuredContent === undefined ? { fullText } : {}),
        },
    };
}

export function failedResult(result: ReturnType<typeof mapResult>) {
    return { ...result, isError: true };
}
