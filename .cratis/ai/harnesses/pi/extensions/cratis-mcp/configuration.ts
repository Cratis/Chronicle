// cratis-ai-managed: harnesses/pi/extensions/cratis-mcp/configuration.ts
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { lstatSync, readFileSync, realpathSync } from 'node:fs';
import { isAbsolute, join, relative, resolve, sep } from 'node:path';

export function object(value: unknown): value is Record<string, unknown> {
    return value !== null && typeof value === 'object' && !Array.isArray(value);
}

export function readJson(path: string): Record<string, unknown> {
    if (lstatSync(path).size > 1024 * 1024) throw new Error(`Cratis configuration is too large: ${path}`);
    const value: unknown = JSON.parse(readFileSync(path, 'utf8'));
    if (!object(value)) throw new Error(`Expected a JSON object: ${path}`);
    return value;
}

function strings(value: unknown): string[] {
    if (value === undefined) return [];
    if (!Array.isArray(value) || !value.every(item => typeof item === 'string')) throw new Error('Expected a Cratis profile/language list.');
    return value;
}

export function supportedSchema(version: unknown): boolean {
    return typeof version === 'string' && /^1\.\d+$/.test(version);
}

/** Resolve through existing directories only, refusing every source-root symlink. Never creates a directory. */
export function physicalRoot(project: string, root: string): string {
    if (!root || isAbsolute(root) || root.includes('\0')) throw new Error('Screenplay root must be a nonempty project-relative directory.');
    const physicalProject = realpathSync(project);
    const candidate = resolve(physicalProject, root);
    const local = relative(physicalProject, candidate);
    if (!local || local === '..' || local.startsWith(`..${sep}`) || isAbsolute(local)) {
        throw new Error('Screenplay root must be strictly inside the project.');
    }
    let current = physicalProject;
    for (const part of local.split(sep)) {
        current = join(current, part);
        let info;
        try {
            info = lstatSync(current);
        } catch (error) {
            if (object(error) && error.code === 'ENOENT') throw new Error('Screenplay root is absent. Run cratis ai install or explicitly create the configured model directory; discovery never creates it.');
            throw error;
        }
        if (info.isSymbolicLink() || !info.isDirectory()) throw new Error('Screenplay root must contain only physical directories, not symlinks.');
    }
    return current;
}

export interface ProfileResolution {
    configuration: Record<string, unknown>;
    selected: Set<string>;
    /** Profiles listed directly in `.cratis/ai.json`, before any `composes` expansion. */
    explicit: Set<string>;
    nonApplicationSelection: boolean;
}

/** Resolve the selected profiles (after `composes` expansion) from the project's `.cratis/ai.json`. */
export function resolveProfiles(project: string, corpus: string): ProfileResolution | undefined {
    let configuration: Record<string, unknown>;
    try {
        configuration = readJson(join(project, '.cratis', 'ai.json'));
    } catch (error) {
        if (object(error) && error.code === 'ENOENT') return undefined;
        throw error;
    }
    const requested = strings(configuration.profiles);
    if (!requested.length) return undefined;
    let catalog: Record<string, unknown>;
    try {
        catalog = readJson(join(corpus, 'profile-catalog.json'));
    } catch (error) {
        if (!object(error) || error.code !== 'ENOENT') throw error;
        catalog = readJson(join(corpus, '..', 'profile-catalog.json'));
    }
    const profiles = [catalog.publicProfiles, catalog.engineeringProfiles].flatMap(group => {
        if (!Array.isArray(group) || !group.every(object)) throw new Error('Invalid Cratis profile catalog.');
        return group;
    });
    const languages = new Set(strings(configuration.languages));
    const selected = new Set<string>();
    const visited = new Set<string>();
    let nonApplicationSelection = false;
    const select = (id: string, explicit: boolean, throughApplication = false): void => {
        const profile = profiles.find(candidate => candidate.id === id);
        if (!profile) throw new Error(`Unknown Cratis AI profile '${id}'.`);
        const supported = strings(profile.languages);
        if (!explicit && supported.length && !supported.some(language => language === 'language-agnostic' || languages.has(language))) return;
        const applicationPath = throughApplication || id === 'cratis/application' || id.startsWith('cratis/application/');
        // Visit both paths: application composition must not hide an existing, independently selected Screenplay path.
        const key = JSON.stringify([id, applicationPath]);
        if (visited.has(key)) return;
        visited.add(key);
        selected.add(id);
        if (id === 'cratis/screenplay' && !applicationPath) nonApplicationSelection = true;
        strings(profile.composes).forEach(child => select(child, false, applicationPath));
    };
    requested.forEach(id => select(id, true));
    return { configuration, selected, explicit: new Set(requested), nonApplicationSelection };
}

/** Select only the shipped Screenplay descriptor, never project-supplied commands or environments. */
export function selectedServer(project: string, corpus: string) {
    const resolution = resolveProfiles(project, corpus);
    if (!resolution?.selected.has('cratis/screenplay')) return undefined;
    const { configuration, nonApplicationSelection } = resolution;
    const overrides = configuration.mcpServers;
    if (overrides !== undefined && !object(overrides)) throw new Error('Invalid mcpServers configuration.');
    const screenplay = object(overrides) ? overrides.screenplay : undefined;
    if (screenplay !== undefined && !object(screenplay)) throw new Error('Invalid Screenplay MCP configuration.');
    if (object(screenplay) && screenplay.enabled === false) return undefined;
    if (object(screenplay) && screenplay.enabled !== undefined && screenplay.enabled !== true) throw new Error('Screenplay MCP enabled must be a boolean.');
    const descriptor = readJson(join(corpus, 'mcp-servers.json'));
    // Any 1.x descriptor is accepted; another major loads no corpus MCP servers (the generic bridge reports it once).
    if (!supportedSchema(descriptor.schemaVersion)) return undefined;
    if (!Array.isArray(descriptor.servers)) throw new Error('Unsupported Cratis MCP descriptor.');
    const servers = descriptor.servers.filter(server => object(server) && server.id === 'screenplay');
    if (servers.length !== 1 || !object(servers[0])) throw new Error('Expected exactly one Screenplay MCP descriptor.');
    const server = servers[0];
    if (server.transport !== 'stdio' || server.command !== 'cratis' || JSON.stringify(server.args) !== '["screenplay","mcp"]' ||
        JSON.stringify(server.profiles) !== '["cratis/screenplay"]' || server.env !== undefined) {
        throw new Error('Only the distributed native Cratis Screenplay MCP command is supported.');
    }
    // Without an explicit root the Cratis CLI locates the model itself: the existing .play files, else Source/src, else Screenplay/.
    const root = object(screenplay) ? screenplay.root : undefined;
    if (root !== undefined && typeof root !== 'string') throw new Error('Screenplay MCP root must be a project-relative string.');
    const physicalProject = realpathSync(project);
    return {
        project: physicalProject,
        root: root === undefined ? undefined : physicalRoot(physicalProject, root),
        composedOnly: !nonApplicationSelection && root === undefined,
    };
}
