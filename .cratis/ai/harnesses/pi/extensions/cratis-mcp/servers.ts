// cratis-ai-managed: harnesses/pi/extensions/cratis-mcp/servers.ts
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { join } from 'node:path';
import { object, readJson, resolveProfiles, supportedSchema } from './configuration.ts';
import type { ServerSelection } from './CorpusServer.ts';

const validId = /^[A-Za-z0-9_-]+$/;

function isSelected(entry: Record<string, unknown>, selected: Set<string>): boolean {
    return Array.isArray(entry.profiles) && entry.profiles.some(profile => typeof profile === 'string' && selected.has(profile));
}

function problem(entry: Record<string, unknown>): string | undefined {
    if (typeof entry.id !== 'string' || !validId.test(entry.id)) return 'the id must match ^[A-Za-z0-9_-]+$';
    if (entry.transport !== 'stdio') return 'only the stdio transport is supported';
    if (typeof entry.command !== 'string' || !entry.command) return 'the command is missing or empty';
    if (!Array.isArray(entry.args) || !entry.args.every(argument => typeof argument === 'string')) return 'args must be an array of strings';
    if (entry.env !== undefined && (!object(entry.env) || !Object.values(entry.env).every(value => typeof value === 'string'))) {
        return 'env must be an object of literal string values';
    }
    return undefined;
}

type Override = { enabled: boolean; eager: boolean } | { error: string };

/** `mcpServers.<id>`: `enabled: false` opts out, `eager: true` registers at session start; a non-boolean is an error for that server only. */
function readOverride(overrides: unknown, id: string): Override {
    const override = object(overrides) ? overrides[id] : undefined;
    if (override === undefined) return { enabled: true, eager: false };
    if (!object(override)) return { error: 'the mcpServers configuration must be an object' };
    if (override.enabled === false) return { enabled: false, eager: false };
    const error = invalidFlag(override.enabled, 'enabled') ?? invalidFlag(override.eager, 'eager');
    return error ? { error } : { enabled: true, eager: override.eager === true };
}

function invalidFlag(value: unknown, name: string): string | undefined {
    return value === undefined || typeof value === 'boolean' ? undefined : `mcpServers ${name} must be a boolean`;
}

/** Select every stdio server whose profiles intersect the explicitly listed profiles (never composed ones), except Screenplay which keeps its own path. */
export function selectedCorpusServers(project: string, corpus: string): ServerSelection {
    const selection: ServerSelection = { servers: [], messages: [] };
    const resolution = resolveProfiles(project, corpus);
    if (!resolution) return selection;
    const descriptor = readJson(join(corpus, 'mcp-servers.json'));
    if (!supportedSchema(descriptor.schemaVersion)) {
        selection.messages.push(`Cratis MCP descriptor schemaVersion '${String(descriptor.schemaVersion)}' is not supported; no corpus MCP servers were loaded.`);
        return selection;
    }
    if (!Array.isArray(descriptor.servers)) {
        selection.messages.push('Cratis MCP descriptor has no servers list; no corpus MCP servers were loaded.');
        return selection;
    }
    const overrides = resolution.configuration.mcpServers;
    if (overrides !== undefined && !object(overrides)) {
        selection.messages.push('Invalid mcpServers configuration; no corpus MCP servers were loaded.');
        return selection;
    }
    const entries = descriptor.servers.filter(object);
    const counts = new Map<unknown, number>();
    entries.forEach(entry => counts.set(entry.id, (counts.get(entry.id) ?? 0) + 1));
    const reported = new Set<unknown>();
    for (const entry of entries) {
        if (entry.id === 'screenplay') continue;
        // An entry without profiles can never be selected, so it is ignored like any other unselected entry.
        if (!isSelected(entry, resolution.explicit)) continue;
        const label = `MCP server '${String(entry.id)}'`;
        if (counts.get(entry.id)! > 1) {
            if (!reported.has(entry.id)) selection.messages.push(`${label} was skipped: the descriptor declares the id more than once.`);
            reported.add(entry.id);
            continue;
        }
        const invalid = problem(entry);
        if (invalid) { selection.messages.push(`${label} was skipped: ${invalid}.`); continue; }
        const id = entry.id as string;
        const override = readOverride(overrides, id);
        if ('error' in override) { selection.messages.push(`${label} was skipped: ${override.error}.`); continue; }
        if (!override.enabled) continue;
        selection.servers.push({ id, command: entry.command as string, args: entry.args as string[], env: { ...(entry.env as Record<string, string> | undefined) }, eager: override.eager });
    }
    return selection;
}
