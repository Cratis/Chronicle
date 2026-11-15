// cratis-ai-managed: harnesses/pi/extensions/cratis-mcp/process.ts
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { execFile, spawn } from 'node:child_process';
import { existsSync } from 'node:fs';
import { dirname, join } from 'node:path';
import type { CorpusServer } from './CorpusServer.ts';
import { StdioConnection } from './StdioConnection.ts';

/** Preserve OS/runtime lookup only; never copy arbitrary credentials or descriptor environment values. */
export function childEnvironment(source: NodeJS.ProcessEnv = process.env): NodeJS.ProcessEnv {
    const environment: NodeJS.ProcessEnv = {};
    for (const key of ['PATH', 'HOME', 'USERPROFILE', 'SystemRoot', 'WINDIR', 'TEMP', 'TMP', 'TMPDIR', 'DOTNET_ROOT', 'DOTNET_ROOT_X64', 'DOTNET_ROOT_ARM64']) {
        if (source[key] !== undefined) environment[key] = source[key];
    }
    return environment;
}

/** Older CLIs create a model root at server startup; optional composition must remain footprint-free. */
export function supportsNoModelStartup(project: string, execute: typeof execFile = execFile): Promise<boolean> {
    return new Promise(resolve => {
        execute('cratis', ['--version'], {
            cwd: project,
            env: childEnvironment(),
            shell: false,
            windowsHide: true,
            timeout: 3000,
            killSignal: 'SIGKILL',
            maxBuffer: 4096,
            encoding: 'utf8',
        }, (error, stdout) => {
            const version = /^\s*(\d+)\.(\d+)\.(\d+)(?:\+[\w.-]+)?\s*$/.exec(stdout);
            if (error || !version) { resolve(false); return; }
            const [, major, minor, patch] = version.map(Number);
            resolve(major > 3 || (major === 3 && (minor > 46 || (minor === 46 && patch >= 1))));
        });
    });
}

export function startConnection(project: string, launch: typeof spawn = spawn): StdioConnection {
    return new StdioConnection(launch('cratis', ['screenplay', 'mcp', '--project-root', project], {
        cwd: project,
        env: childEnvironment(),
        shell: false,
        stdio: ['pipe', 'pipe', 'pipe'],
        windowsHide: true,
    }));
}

/** Launch a corpus server without a shell or interpolation; Windows npx uses npm's JavaScript entry point. */
export function startServer(project: string, server: CorpusServer, launch: typeof spawn = spawn, platform: NodeJS.Platform = process.platform, fileExists: (path: string) => boolean = existsSync): StdioConnection {
    let command = server.command;
    let args = server.args;
    if (platform === 'win32' && /^npx(?:\.cmd)?$/i.test(command)) {
        const entryPoint = join(dirname(process.execPath), 'node_modules', 'npm', 'bin', 'npx-cli.js');
        if (!fileExists(entryPoint)) {
            throw new Error(`MCP server '${server.id}' requires npm's npx-cli.js at '${entryPoint}'. Install Node.js with npm; Windows command shims cannot be launched without a shell.`);
        }
        command = process.execPath;
        args = [entryPoint, ...args];
    }
    return new StdioConnection(launch(command, args, {
        cwd: project,
        env: { ...childEnvironment(), ...server.env },
        shell: false,
        stdio: ['pipe', 'pipe', 'pipe'],
        windowsHide: true,
    }), 30_000, server.id, true);
}
