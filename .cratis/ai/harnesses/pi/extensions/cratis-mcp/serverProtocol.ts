// cratis-ai-managed: harnesses/pi/extensions/cratis-mcp/serverProtocol.ts
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { TSchema } from 'typebox';
import { object } from './configuration.ts';
import type { StdioConnection } from './StdioConnection.ts';

/** A tool of a corpus server other than Screenplay, exposed under a name namespaced by the server id. */
export interface ServerTool {
    name: string;
    nativeName: string;
    description: string;
    parameters: TSchema;
}

export interface ServerCatalog {
    tools: ServerTool[];
    messages: string[];
}

const maximumTools = 128;
const maximumPages = 8;
const maximumNativeNameLength = 64;
const maximumDescriptionLength = 4 * 1024;
const maximumSchemaLength = 64 * 1024;
const maximumImageLength = 2 * 1024 * 1024;
const imageTypes = new Set(['image/png', 'image/jpeg', 'image/gif', 'image/webp']);

/** Initialize and list tools; tools that cannot be exposed safely are skipped with a message rather than failing the server. */
export async function discoverServer(connection: StdioConnection, id: string): Promise<ServerCatalog> {
    const initialized = await connection.request('initialize', {
        protocolVersion: '2025-06-18', capabilities: {}, clientInfo: { name: 'cratis.pi', version: '1.0.0' },
    });
    if (!object(initialized) || !object(initialized.capabilities) || !object(initialized.capabilities.tools)) {
        throw new Error(`MCP server '${id}' did not advertise tool support.`);
    }
    connection.notify('notifications/initialized');
    const listed: unknown[] = [];
    let cursor: unknown;
    for (let page = 0; page < maximumPages; page++) {
        const response = await connection.request('tools/list', cursor === undefined ? {} : { cursor });
        if (!object(response) || !Array.isArray(response.tools)) throw new Error(`MCP server '${id}' returned an invalid tool list.`);
        listed.push(...response.tools);
        if (listed.length > maximumTools) throw new Error(`MCP server '${id}' exposes more than ${maximumTools} tools.`);
        cursor = response.nextCursor;
        if (cursor === undefined) break;
        if (typeof cursor !== 'string') throw new Error(`MCP server '${id}' returned an invalid pagination cursor.`);
    }
    const catalog: ServerCatalog = { tools: [], messages: [] };
    const names = new Set<string>();
    for (const tool of listed) {
        const name = object(tool) ? tool.name : undefined;
        if (!object(tool) || typeof name !== 'string' || !name || !object(tool.inputSchema) || tool.inputSchema.type !== 'object' ||
            (tool.description !== undefined && typeof tool.description !== 'string')) {
            catalog.messages.push(`MCP server '${id}' has a tool with an invalid name or input schema; it was skipped.`);
            continue;
        }
        if (JSON.stringify(tool.inputSchema).length > maximumSchemaLength || ((tool.description as string | undefined)?.length ?? 0) > maximumDescriptionLength) {
            catalog.messages.push(`MCP server '${id}' tool '${name}' was skipped: its description or input schema is too large.`);
            continue;
        }
        const nativeName = `${id}_${name.replace(/[^A-Za-z0-9_]/g, '_')}`;
        if (nativeName.length > maximumNativeNameLength || names.has(nativeName)) {
            catalog.messages.push(`MCP server '${id}' tool '${name}' was skipped: its namespaced name is too long or not unique.`);
            continue;
        }
        names.add(nativeName);
        catalog.tools.push({ name, nativeName, description: (tool.description as string | undefined) ?? '', parameters: tool.inputSchema as TSchema });
    }
    if (!catalog.tools.length) throw new Error(`MCP server '${id}' exposes no usable tools.`);
    return catalog;
}

/** Map an MCP result to Pi content. Server-reported errors throw so Pi marks the call failed. */
export function mapServerResult(result: unknown, id: string) {
    if (!object(result) || !Array.isArray(result.content) || (result.isError !== undefined && typeof result.isError !== 'boolean')) {
        throw new Error(`Malformed MCP result from '${id}'.`);
    }
    const content = result.content.map((part: unknown) => {
        if (object(part) && part.type === 'text' && typeof part.text === 'string') return { type: 'text' as const, text: part.text };
        if (object(part) && part.type === 'image' && typeof part.data === 'string' && typeof part.mimeType === 'string') {
            if (!imageTypes.has(part.mimeType) || part.data.length > maximumImageLength) return { type: 'text' as const, text: `[image omitted: unsupported type or larger than ${maximumImageLength / 1024 / 1024} MiB]` };
            return { type: 'image' as const, data: part.data, mimeType: part.mimeType };
        }
        return { type: 'text' as const, text: `[unsupported ${object(part) ? String(part.type) : 'unknown'} content omitted]` };
    });
    const maximumCharacters = 32 * 1024;
    const bounded = content.map(part => part.type === 'text' && part.text.length > maximumCharacters
        ? { ...part, text: `${part.text.slice(0, maximumCharacters)}\n[display truncated]` }
        : part);
    return { content: bounded, details: { isError: result.isError === true, structuredContent: result.structuredContent } };
}
