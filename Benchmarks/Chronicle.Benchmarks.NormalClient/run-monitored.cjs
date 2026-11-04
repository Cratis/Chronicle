// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
const { spawn } = require('node:child_process');
const { loadavg } = require('node:os');
const fs = require('node:fs');

// One owned foreground process; no external-job polling. Record load every second plus both boundaries.
const [log, cwd, command, ...args] = process.argv.slice(2);
// Load-invalidation threshold; LOAD_LIMIT overrides the default of 15 (e.g. a declared perf window).
const loadLimit = process.env.LOAD_LIMIT === undefined ? 15 : Number(process.env.LOAD_LIMIT);
if (!Number.isFinite(loadLimit) || loadLimit <= 0) { console.error(`invalid LOAD_LIMIT=${process.env.LOAD_LIMIT}`); process.exit(2); }
let fd;
try { fd = fs.openSync(log, 'wx'); } catch (error) { console.error(error.message); process.exit(2); }
let child, invalid = false, cancelled = false, killTimer;
function stop() {
    if (!child || killTimer) return;
    if (!child.pid) return;
    try { process.kill(-child.pid, 'SIGTERM'); } catch (error) { if (error.code !== 'ESRCH') throw error; }
    killTimer = setTimeout(() => {
        try { process.kill(-child.pid, 'SIGKILL'); } catch (error) { if (error.code !== 'ESRCH') throw error; }
    }, 5000);
}
function record() {
    const load = loadavg()[0];
    fs.writeSync(fd, JSON.stringify({ timestamp: new Date().toISOString(), load, loadLimit }) + '\n');
    if (!Number.isFinite(load) || load >= loadLimit) {
        invalid = true;
        console.error(`load=${load}: invalidating process`);
        stop();
    }
}
record();
if (invalid) { fs.closeSync(fd); process.exit(3); }
child = spawn(command, args, { cwd, stdio: 'inherit', detached: true });
const interval = setInterval(record, 1000);
const timeout = setTimeout(() => { cancelled = true; console.error('process timeout (600s)'); stop(); }, 600000);
for (const signal of ['SIGINT', 'SIGTERM']) process.on(signal, () => { cancelled = true; stop(); });
child.on('error', error => { console.error(error.message); });
child.on('close', (code, signal) => {
    clearInterval(interval);
    clearTimeout(timeout);
    clearTimeout(killTimer);
    // The process is gone: the boundary sample must not attempt cancellation.
    child = undefined;
    record();
    fs.closeSync(fd);
    console.error(`process end code=${code} signal=${signal} invalid=${invalid} cancelled=${cancelled}`);
    process.exitCode = invalid ? 3 : cancelled ? 130 : code === 0 ? 0 : 2;
});
