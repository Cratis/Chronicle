// cratis-ai-managed: harnesses/pi/extensions/cratis-mcp/process.ts
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { execFile, spawn } from 'node:child_process';
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
