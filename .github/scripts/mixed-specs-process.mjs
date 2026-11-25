// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { spawn } from 'node:child_process';
import { constants } from 'node:os';
import { SpecHangWatchdog } from './spec-hang-watchdog.mjs';

function killProcessGroup(pid) {
    if (!pid) return;
    // A group whose members already exited: Linux reports ESRCH, macOS EPERM.
    try { process.kill(-pid, 'SIGKILL'); } catch (error) { if (error.code !== 'ESRCH' && error.code !== 'EPERM') throw error; }
}

// Own one child group and its complete deadline/signal/output lifecycle. No shell,
// eval, retries or collector IPC timeout pretending to be a test deadline.
class ChildProcessGroup {
    stdout = '';
    stderr = '';
    expired = false;
    failure;

    constructor(command, args, { cwd, timeout, capture, specProgress }) {
        this.command = command;
        this.progress = specProgress ? new SpecHangWatchdog(specProgress) : undefined;
        this.child = spawn(command, args, { cwd, detached: true, stdio: capture || specProgress ? ['ignore', 'pipe', 'pipe'] : 'inherit' });
        this.progress?.watch(this.child.stdout, error => this.abort(error));
        if (specProgress && !capture) this.child.stderr.on('data', data => process.stderr.write(data));
        this.onTerm = () => this.interrupt('SIGTERM');
        this.onInt = () => this.interrupt('SIGINT');
        process.once('SIGTERM', this.onTerm);
        process.once('SIGINT', this.onInt);
        this.timer = setTimeout(() => this.expire(), timeout);
        if (capture) {
            this.capture(this.child.stdout, 'stdout');
            this.capture(this.child.stderr, 'stderr');
        }
    }

    killTree() {
        try { killProcessGroup(this.child.pid); } catch (error) { this.failure = error; }
    }

    abort(error) {
        this.failure = error;
        this.killTree();
    }

    interrupt(signal) {
        this.abort(Object.assign(new Error(`Interrupted: ${signal}`), { exitCode: 128 + constants.signals[signal] }));
    }

    expire() {
        this.expired = true;
        try { this.progress?.summary('TestSessionTimeout exceeded; terminating process tree (exit 124)'); }
        catch (error) { this.failure = error; }
        this.killTree();
    }

    capture(stream, channel) {
        stream?.on('data', data => this.append(channel, data));
    }

    append(channel, data) {
        this[channel] += data.toString();
        if (this.stdout.length + this.stderr.length > 16 * 1024 * 1024) {
            this.failure = new Error('Child output exceeded 16 MiB');
            this.killTree();
        }
    }

    close(code, signal, resolve, reject) {
        clearTimeout(this.timer);
        this.progress?.stop();
        this.killTree();
        process.removeListener('SIGTERM', this.onTerm);
        process.removeListener('SIGINT', this.onInt);
        if (this.expired) this.failure = Object.assign(new Error(`Deadline exceeded: ${this.command}`), { exitCode: 124 });
        if (this.failure) return reject(this.failure);
        resolve({ code: code ?? 128 + (constants.signals[signal] ?? 0), stdout: this.stdout, stderr: this.stderr });
    }

    wait(resolve, reject) {
        this.child.on('error', error => { this.failure = error; });
        this.child.on('close', (code, signal) => this.close(code, signal, resolve, reject));
    }
}

export function runProcess(command, args, { cwd, timeout = 1800000, capture = false, specProgress } = {}) {
    return new Promise((resolve, reject) => {
        if (process.platform === 'win32') return reject(new Error('Process-tree deadlines require a POSIX runner'));
        new ChildProcessGroup(command, args, { cwd, timeout, capture, specProgress }).wait(resolve, reject);
    });
}
