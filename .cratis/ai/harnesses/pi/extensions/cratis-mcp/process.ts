// cratis-ai-managed: harnesses/pi/extensions/cratis-mcp/process.ts
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { spawn } from 'node:child_process';
import { StdioConnection } from './StdioConnection.ts';

/** Preserve OS/runtime lookup only; never copy arbitrary credentials or descriptor environment values. */
export function childEnvironment(source: NodeJS.ProcessEnv = process.env): NodeJS.ProcessEnv {
    const environment: NodeJS.ProcessEnv = {};
    for (const key of ['PATH', 'HOME', 'USERPROFILE', 'SystemRoot', 'WINDIR', 'TEMP', 'TMP', 'TMPDIR', 'DOTNET_ROOT', 'DOTNET_ROOT_X64', 'DOTNET_ROOT_ARM64']) {
        if (source[key] !== undefined) environment[key] = source[key];
    }
    return environment;
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
