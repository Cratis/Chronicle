// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Runs the real Docker/entrypoint.sh against a fake server, so the signal forwarding and the exit status
// the container reports are proven without an image. SQL storage skips the embedded mongod, and the
// port diagnostics degrade to a stub outside the image. local-entrypoint.sh and pr_entrypoint.sh always
// start mongod, so they are held to the same supervision block instead (see #5361).
import test from 'node:test';
import assert from 'node:assert/strict';
import { spawn } from 'node:child_process';
import { chmod, mkdtemp, readFile, rm, writeFile } from 'node:fs/promises';
import { existsSync } from 'node:fs';
import os from 'node:os';
import path from 'node:path';

const docker = path.resolve('Docker');
const entrypoint = path.join(docker, 'entrypoint.sh');
const deadline = 20000;

// Traps TERM like the .NET host: it logs the signal, takes a moment to drain, then exits. It started in
// the background, so SIGINT is ignored and cannot be trapped, exactly as for the real server.
// `sleep & wait` keeps the trap responsive; a foreground sleep would defer it. The loop ends on its own,
// so a failed spec never leaves a server behind.
const fakeServer = `#!/usr/bin/env bash
stop() {
  echo "$1" >> signals.log
  sleep "\${STOP_DELAY:-0.2}"
  exit "\${STOP_STATUS:-0}"
}
trap 'stop TERM' TERM
touch ready
if [ -n "\${EXIT_ON_ITS_OWN:-}" ]; then
  sleep 0.2
  [ -n "\${SIGNAL_PARENT_ON_EXIT:-}" ] && kill -TERM "$PPID"
  exit "$EXIT_ON_ITS_OWN"
fi
for _ in $(seq 1 30); do
  sleep 1 &
  wait $!
done
exit 99
`;

async function workspace() {
    const directory = await mkdtemp(path.join(os.tmpdir(), 'chronicle-entrypoint-'));
    const server = path.join(directory, 'Cratis.Chronicle.Server');
    await writeFile(server, fakeServer);
    await chmod(server, 0o755);
    return directory;
}

async function waitFor(file) {
    const started = Date.now();
    while (!existsSync(file)) {
        if (Date.now() - started > deadline) throw new Error(`Timed out waiting for ${file}`);
        await new Promise(resolve => setTimeout(resolve, 20));
    }
}

async function run(environment, act = async () => {}) {
    const directory = await workspace();
    try {
        const child = spawn('bash', [entrypoint], {
            cwd: directory,
            env: { ...process.env, Cratis__Chronicle__Storage__Type: 'MsSql', ...environment },
            stdio: ['ignore', 'pipe', 'pipe'],
        });
        let output = '';
        child.stdout.on('data', chunk => { output += chunk; });
        child.stderr.on('data', chunk => { output += chunk; });
        const exited = new Promise(resolve => child.on('exit', (code, signal) => resolve({ code, signal })));
        const timer = setTimeout(() => child.kill('SIGKILL'), deadline);
        await waitFor(path.join(directory, 'ready'));
        await act(child);
        const result = await exited;
        clearTimeout(timer);
        const log = path.join(directory, 'signals.log');
        const signals = existsSync(log) ? (await readFile(log, 'utf8')).trim().split('\n') : [];
        return { ...result, signals, output };
    } finally {
        await rm(directory, { recursive: true, force: true });
    }
}

test('forwards TERM to the server and exits with its status', async () => {
    const result = await run({}, async child => child.kill('SIGTERM'));
    assert.deepEqual(result.signals, ['TERM'], result.output);
    assert.equal(result.code, 0, result.output);
});

test('forwards INT to the server as TERM', async () => {
    const result = await run({}, async child => child.kill('SIGINT'));
    assert.deepEqual(result.signals, ['TERM'], result.output);
    assert.equal(result.code, 0, result.output);
});

test('reports a failing shutdown with the server status, not the signal', async () => {
    const result = await run({ STOP_STATUS: '3' }, async child => child.kill('SIGTERM'));
    assert.equal(result.code, 3, result.output);
});

test('keeps the server status when TERM is repeated while it drains', async () => {
    const result = await run({ STOP_DELAY: '0.6' }, async child => {
        child.kill('SIGTERM');
        await new Promise(resolve => setTimeout(resolve, 150));
        child.kill('SIGTERM');
    });
    assert.equal(result.code, 0, result.output);
});

test('exits with the server status when the server exits on its own', async () => {
    const result = await run({ EXIT_ON_ITS_OWN: '5' });
    assert.deepEqual(result.signals, [], result.output);
    assert.equal(result.code, 5, result.output);
});

// The signal lands either while wait -n is still waiting or just after it reaped the server; both
// orders must report the server's own status rather than 127 or 128 + 15.
test('keeps the server status when TERM arrives as the server exits', async () => {
    for (let attempt = 0; attempt < 5; attempt++) {
        const result = await run({ EXIT_ON_ITS_OWN: '4', SIGNAL_PARENT_ON_EXIT: '1' });
        assert.equal(result.code, 4, `attempt ${attempt}: ${result.output}`);
    }
});

// Up to the end of the re-wait loop: what follows only stops mongod, which pr_entrypoint.sh always starts.
function supervision(script) {
    const start = script.indexOf('server_pid=""');
    const end = script.indexOf('\n  done\n', start);
    assert.ok(start >= 0 && end > start, 'supervision block not found');
    return script.slice(start, end);
}

test('local and PR entrypoints supervise the server the same way', async () => {
    const expected = supervision(await readFile(entrypoint, 'utf8'));
    for (const name of ['local-entrypoint.sh', 'pr_entrypoint.sh']) {
        assert.equal(supervision(await readFile(path.join(docker, name), 'utf8')), expected, name);
    }
});
