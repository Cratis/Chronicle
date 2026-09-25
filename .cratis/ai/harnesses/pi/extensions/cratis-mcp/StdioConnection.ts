// cratis-ai-managed: harnesses/pi/extensions/cratis-mcp/StdioConnection.ts
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { ChildProcessWithoutNullStreams } from 'node:child_process';
import { ConnectionFailure } from './ConnectionFailure.ts';
import { object } from './configuration.ts';
import type { PendingRequest } from './PendingRequest.ts';

/** Bounded newline JSON-RPC transport for Screenplay's sequential stdio subset. */
export class StdioConnection {
    private _buffer = Buffer.alloc(0);
    private _sequence = 0;
    private _pending?: PendingRequest;
    private _failure?: ConnectionFailure;
    private _termination?: ReturnType<typeof setTimeout>;
    private readonly _maximumBytes = 8 * 1024 * 1024;

    constructor(private readonly _child: ChildProcessWithoutNullStreams, private readonly _timeout = 30_000) {
        _child.stdout.on('data', this.receive);
        _child.stdout.on('end', this.ended);
        _child.stdout.on('error', this.streamFailed);
        _child.stdin.on('error', this.streamFailed);
        // Drain without retaining or showing raw server logs/protocol in the host UI.
        _child.stderr.on('data', this.drain);
        _child.stderr.on('error', this.streamFailed);
        _child.on('error', this.spawnFailed);
        _child.once('close', this.closed);
    }

    get stopped(): boolean { return this._failure !== undefined; }
    get outcomeUnknown(): boolean { return this._failure?.outcomeUnknown === true; }

    async request(method: string, params: Record<string, unknown>, signal?: AbortSignal, mutation = false): Promise<unknown> {
        if (this._failure) throw this._failure;
        if (signal?.aborted) throw new ConnectionFailure('Screenplay request cancelled before sending.');
        if (this._pending) throw new ConnectionFailure('Screenplay is busy; parallel requests are not queued.');
        const id = ++this._sequence;
        const line = this.encode({ jsonrpc: '2.0', id, method, params });
        return new Promise((resolve, reject) => {
            const abort = () => this.stop('Screenplay request cancelled after sending.');
            const timer = setTimeout(() => this.stop('Screenplay request timed out.'), this._timeout);
            this._pending = {
                id, mutation, resolve, reject,
                cleanup: () => { clearTimeout(timer); signal?.removeEventListener('abort', abort); },
            };
            signal?.addEventListener('abort', abort, { once: true });
            try {
                this._child.stdin.write(line, error => { if (error) this.stop('Screenplay input pipe failed.'); });
            } catch {
                this.stop('Screenplay input pipe failed.');
            }
        });
    }

    notify(method: string): void {
        if (this._failure) throw this._failure;
        try {
            this._child.stdin.write(this.encode({ jsonrpc: '2.0', method }), error => { if (error) this.stop('Screenplay notification failed.'); });
        } catch {
            this.stop('Screenplay notification failed.');
            throw this._failure;
        }
    }

    dispose(): void { this.stop('Screenplay session closed.'); }

    private encode(value: unknown): string {
        const line = `${JSON.stringify(value)}\n`;
        if (Buffer.byteLength(line) > this._maximumBytes) throw new ConnectionFailure('Screenplay request exceeds the 8 MiB transport limit.');
        return line;
    }

    private readonly receive = (chunk: Buffer): void => {
        if (this.stopped) return;
        // Stream chunks can contain many lines; bound each unfinished line before concatenation.
        let start = 0;
        for (let index = 0; index < chunk.length; index++) {
            if (chunk[index] !== 10) continue;
            if (!this.append(chunk.subarray(start, index))) return;
            try {
                const line = new TextDecoder('utf-8', { fatal: true }).decode(this._buffer);
                this._buffer = Buffer.alloc(0);
                this.accept(line);
            } catch {
                this.stop('Screenplay returned invalid UTF-8 protocol data.');
            }
            if (this.stopped) return;
            start = index + 1;
        }
        this.append(chunk.subarray(start));
    };

    private append(chunk: Buffer): boolean {
        if (this._buffer.length + chunk.length > this._maximumBytes) {
            this.stop('Screenplay response exceeds the 8 MiB transport limit.');
            return false;
        }
        this._buffer = Buffer.concat([this._buffer, chunk]);
        return true;
    }

    private accept(line: string): void {
        try {
            const message: unknown = JSON.parse(line);
            if (!object(message) || message.jsonrpc !== '2.0') throw new Error();
            if ('method' in message) {
                // This pinned server does not send requests or changing tool lists.
                throw new Error();
            }
            const pending = this._pending;
            if (!pending || message.id !== pending.id || ('result' in message) === ('error' in message)) throw new Error();
            if ('error' in message && (!object(message.error) || typeof message.error.code !== 'number' || typeof message.error.message !== 'string')) throw new Error();
            this._pending = undefined;
            pending.cleanup();
            if (object(message.error)) {
                pending.reject(new ConnectionFailure(`Screenplay protocol error ${message.error.code}: ${message.error.message}`, pending.mutation));
                // A server failure during mutation is not evidence that nothing was written.
                if (pending.mutation) this.stop('Screenplay mutation failed with a protocol error.', true);
            } else {
                pending.resolve(message.result);
            }
        } catch {
            this.stop('Screenplay returned malformed or unexpected JSON-RPC.');
        }
    }

    private stop(message: string, outcomeUnknown = this._pending?.mutation === true): void {
        if (this._failure) return;
        this._failure = new ConnectionFailure(message, outcomeUnknown);
        const pending = this._pending;
        this._pending = undefined;
        pending?.cleanup();
        pending?.reject(this._failure);
        this._buffer = Buffer.alloc(0);
        this._child.stdout.removeListener('data', this.receive);
        this._child.stdout.removeListener('end', this.ended);
        this._child.stdout.resume();
        this._child.stdin.end();
        if (this._child.exitCode === null && this._child.signalCode === null) {
            this._child.kill('SIGTERM');
            // Deadline escalation, not a completion wait. Cleared on the child's close signal.
            this._termination = setTimeout(() => this._child.kill('SIGKILL'), 1000);
            this._termination.unref();
        }
    }

    private readonly drain = (): void => { /* Drain server stderr without exposing it as a protocol result. */ };
    private readonly ended = (): void => this.stop('Screenplay output closed before session shutdown.');
    private readonly streamFailed = (): void => this.stop('Screenplay stdio failed.');
    private readonly spawnFailed = (): void => this.stop('Cannot launch native cratis screenplay mcp. Install/update the Cratis CLI through its normal channel; this extension never downloads a runtime.');
    private readonly closed = (): void => {
        this.stop('Screenplay process exited. Check that the installed Cratis CLI includes the embedded Screenplay runtime; no runtime is downloaded by this extension.');
        clearTimeout(this._termination);
        this._child.stdout.removeListener('error', this.streamFailed);
        this._child.stdin.removeListener('error', this.streamFailed);
        this._child.stderr.removeListener('error', this.streamFailed);
        this._child.stderr.removeListener('data', this.drain);
        this._child.removeListener('error', this.spawnFailed);
    };
}
