// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { spawn } from 'node:child_process';
import { constants } from 'node:os';

function killProcessGroup(pid) {
    if (!pid) return;
    try { process.kill(-pid, 'SIGKILL'); } catch (error) { if (error.code !== 'ESRCH') throw error; }
}

// Own one child group and its complete deadline/signal/output lifecycle. No shell,
// eval, retries or collector IPC timeout pretending to be a test deadline.
class ChildProcessGroup {
    stdout = '';
    stderr = '';
    expired = false;
    failure;

    constructor(command, args, { cwd, timeout, capture }) {
        this.command = command;
        this.child = spawn(command, args, { cwd, detached: true, stdio: capture ? ['ignore', 'pipe', 'pipe'] : 'inherit' });
        this.onTerm = () => this.interrupt('SIGTERM');
        this.onInt = () => this.interrupt('SIGINT');
        process.once('SIGTERM', this.onTerm);
        process.once('SIGINT', this.onInt);
        this.timer = setTimeout(() => this.expire(), timeout);
        this.capture(this.child.stdout, 'stdout');
        this.capture(this.child.stderr, 'stderr');
    }

    killTree() {
        try { killProcessGroup(this.child.pid); } catch (error) { this.failure = error; }
    }

    interrupt(signal) {
        this.failure = Object.assign(new Error(`Interrupted: ${signal}`), { exitCode: 128 + constants.signals[signal] });
        this.killTree();
    }

    expire() {
        this.expired = true;
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

export function runProcess(command, args, { cwd, timeout = 1800000, capture = false } = {}) {
    return new Promise((resolve, reject) => {
        if (process.platform === 'win32') return reject(new Error('Process-tree deadlines require a POSIX runner'));
        new ChildProcessGroup(command, args, { cwd, timeout, capture }).wait(resolve, reject);
    });
}
