// cratis-ai-managed: harnesses/pi/extensions/cratis-hooks/quality-gate.ts
/**
 * Bounded, cancellable process execution for the Cratis quality gate in Pi.
 *
 * The gate builds and tests the repository, which can take many minutes and, when a test host
 * hangs, never finish at all. Everything here exists so that a gate run is always bounded: it
 * stops at a deadline or when its caller cancels, the whole process tree is stopped (not just the
 * `bash` running the script), and the caller learns which of those happened rather than seeing a
 * bare exit code. A gate that timed out or was cancelled verified nothing, and is reported as such.
 */

import { spawn } from "node:child_process";
import { createHash } from "node:crypto";
import * as fs from "node:fs";
import * as path from "node:path";
import { StringDecoder } from "node:string_decoder";

/** How long one explicit gate run may take before it is stopped and reported as timed out. */
export const DEFAULT_GATE_TIMEOUT_SECONDS = 300;
export const MIN_GATE_TIMEOUT_SECONDS = 1;
export const MAX_GATE_TIMEOUT_SECONDS = 600;
/** The dispatch plan is `git diff` plus `jq`; anything slower than this is itself a problem. */
export const PLAN_TIMEOUT_MS = 30_000;
export const FINGERPRINT_TIMEOUT_MS = 15_000;

const OUTPUT_CAP = 256 * 1024;
const KILL_GRACE_MS = 5_000;
const CLOSE_GRACE_MS = 2_000;

export interface BoundedRun {
	/** Exit code, or null when the process was stopped by a signal or never started. */
	code: number | null;
	stdout: string;
	/** True when stdout exceeded the retained output cap; never interpret a partial Git status. */
	stdoutTruncated: boolean;
	stderr: string;
	/** Stopped because the deadline passed. */
	timedOut: boolean;
	/** Stopped because the caller's signal aborted. */
	aborted: boolean;
	/** Could not be started at all (or bash could not find the script: exit 127). */
	failed: boolean;
	durationMs: number;
}

export interface BoundedRunOptions {
	cwd: string;
	stdin?: string;
	timeoutMs: number;
	signal?: AbortSignal;
	env?: NodeJS.ProcessEnv;
	/** Receives raw stdout chunks, for callers that hash output instead of keeping it. */
	onStdout?: (chunk: Buffer) => void;
	/** Called every `heartbeatMs` while the process runs. */
	onHeartbeat?: (elapsedMs: number) => void;
	heartbeatMs?: number;
	killGraceMs?: number;
}

function appendCapped(current: string, chunk: string): string {
	const next = current + chunk;
	return next.length > OUTPUT_CAP ? next.slice(next.length - OUTPUT_CAP) : next;
}

/**
 * Run a command with a hard deadline. Never throws and always settles.
 *
 * On POSIX the command runs in its own process group, so stopping it also stops what it started —
 * `dotnet test` and its test hosts, not only the `bash` that launched them. Stopping sends SIGTERM,
 * then SIGKILL after a grace period, and the promise settles even if a descendant keeps a pipe open.
 */
export function runBounded(command: string, args: string[], options: BoundedRunOptions): Promise<BoundedRun> {
	const started = Date.now();
	return new Promise<BoundedRun>((resolve) => {
		const useGroup = process.platform !== "win32";
		let proc: ReturnType<typeof spawn>;
		try {
			proc = spawn(command, args, {
				cwd: options.cwd,
				env: options.env ?? process.env,
				stdio: ["pipe", "pipe", "pipe"],
				detached: useGroup,
			});
		} catch (error) {
			resolve({ code: null, stdout: "", stdoutTruncated: false, stderr: String(error), timedOut: false, aborted: false, failed: true, durationMs: 0 });
			return;
		}

		let stdout = "";
		let stdoutBytes = 0;
		let stderr = "";
		const stdoutDecoder = new StringDecoder("utf8");
		const stderrDecoder = new StringDecoder("utf8");
		let timedOut = false;
		let aborted = false;
		let exitCode: number | null = null;
		let settled = false;
		let stopping = false;
		let closed = false;
		const timers: NodeJS.Timeout[] = [];

		const signalTree = (signal: NodeJS.Signals) => {
			try {
				if (useGroup && proc.pid !== undefined) process.kill(-proc.pid, signal);
				else proc.kill(signal);
			} catch {
				try {
					proc.kill(signal);
				} catch {
					/* already gone */
				}
			}
		};
		const stop = () => {
			stopping = true;
			signalTree("SIGTERM");
			// The parent may close immediately while its children ignore TERM. Never finish before escalation.
			timers.push(setTimeout(() => {
				signalTree("SIGKILL");
				if (closed) finish();
			}, options.killGraceMs ?? KILL_GRACE_MS));
			// Settle even if a descendant holds a pipe open after escalation.
			timers.push(setTimeout(() => finish(), (options.killGraceMs ?? KILL_GRACE_MS) + CLOSE_GRACE_MS));
		};
		const onAbort = () => {
			if (settled || timedOut || aborted) return;
			aborted = true;
			stop();
		};
		const finish = (failed = false) => {
			if (settled) return;
			settled = true;
			stdout = appendCapped(stdout, stdoutDecoder.end());
			stderr = appendCapped(stderr, stderrDecoder.end());
			for (const timer of timers) clearTimeout(timer);
			options.signal?.removeEventListener("abort", onAbort);
			resolve({
				code: exitCode,
				stdout,
				stdoutTruncated: stdoutBytes > OUTPUT_CAP,
				stderr,
				timedOut,
				aborted,
				failed: failed || exitCode === 127,
				durationMs: Date.now() - started,
			});
		};

		proc.stdout?.on("data", (chunk: Buffer) => {
			options.onStdout?.(chunk);
			stdoutBytes += chunk.length;
			stdout = appendCapped(stdout, stdoutDecoder.write(chunk));
		});
		proc.stderr?.on("data", (chunk: Buffer) => (stderr = appendCapped(stderr, stderrDecoder.write(chunk))));
		proc.on("error", (error) => {
			stderr = appendCapped(stderr, String(error));
			finish(true);
		});
		proc.on("exit", (code) => {
			exitCode = code;
			// A descendant that inherited stdout/stderr can keep 'close' from ever firing.
			if (!stopping) timers.push(setTimeout(() => { if (!stopping) finish(); }, CLOSE_GRACE_MS));
		});
		proc.on("close", () => {
			closed = true;
			if (!stopping) finish();
		});

		timers.push(
			setTimeout(() => {
				if (settled || aborted) return;
				timedOut = true;
				stop();
			}, options.timeoutMs),
		);
		if (options.onHeartbeat && options.heartbeatMs) {
			const heartbeat = setInterval(() => {
				if (!settled) options.onHeartbeat?.(Date.now() - started);
			}, options.heartbeatMs);
			timers.push(heartbeat);
		}
		if (options.signal) {
			if (options.signal.aborted) onAbort();
			else options.signal.addEventListener("abort", onAbort, { once: true });
		}

		proc.stdin?.on("error", () => {
			/* the command may exit without reading stdin */
		});
		try {
			proc.stdin?.end(options.stdin ?? "");
		} catch {
			/* ignore */
		}
	});
}

/** Resolve the timeout for one run: explicit request, then the environment, then the default — clamped. */
export function gateTimeoutSeconds(requested?: number, environment = process.env.CRATIS_HOOKS_GATE_TIMEOUT_SECONDS): number {
	const fromEnvironment = environment !== undefined && environment.trim() !== "" ? Number(environment) : undefined;
	const candidate = requested ?? fromEnvironment ?? DEFAULT_GATE_TIMEOUT_SECONDS;
	if (!Number.isFinite(candidate)) return DEFAULT_GATE_TIMEOUT_SECONDS;
	return Math.min(MAX_GATE_TIMEOUT_SECONDS, Math.max(MIN_GATE_TIMEOUT_SECONDS, Math.round(candidate)));
}

/** Unique planned gate labels, in order, with package counts for multi-directory runs. */
export function parseGatePlan(dryRunStderr: string): string[] {
	const directories = new Map<string, Set<string>>();
	for (const match of dryRunStderr.matchAll(/^cratis-quality-gate: RUN[ \t]+(\S+)[^\n]*(?:\n[ \t]+\$[^\n]*\(cwd: (.*)\))?/gm)) {
		const locations = directories.get(match[1]) ?? new Set<string>();
		locations.add(match[2] ?? "unknown");
		directories.set(match[1], locations);
	}
	return [...directories].map(([gate, locations]) => locations.size > 1 ? `${gate} (${locations.size} packages)` : gate);
}

/** Where cratis-quality-gate.sh writes each gate's full log for a session (mirrors hook_state_dir in hook-lib.sh). */
export function gateLogDirectory(sessionId: string, environment = process.env): string {
	const session = sessionId.replace(/[^A-Za-z0-9._-]/g, "_") || "nosession";
	const base = (environment.TMPDIR || "/tmp").replace(/\/+$/, "");
	return path.join(base, "cratis-hooks", session, "gate-logs");
}

/** The gate log written most recently since `sinceMs` — the gate that is running, or was when it stopped. */
export function latestGateLog(directory: string, sinceMs: number): { gate: string; cwd?: string; file: string } | undefined {
	let latest: { gate: string; file: string; mtime: number } | undefined;
	try {
		for (const name of fs.readdirSync(directory)) {
			if (!name.endsWith(".log")) continue;
			const file = path.join(directory, name);
			const mtime = fs.statSync(file).mtimeMs;
			if (mtime + 1000 < sinceMs) continue;
			if (!latest || mtime > latest.mtime) latest = { gate: name.slice(0, -".log".length), file, mtime };
		}
	} catch {
		return undefined;
	}
	if (!latest) return undefined;
	// The script writes identity before command output. Bound the read even for enormous logs.
	let descriptor: number | undefined;
	try {
		descriptor = fs.openSync(latest.file, "r");
		const buffer = Buffer.alloc(8192);
		const length = fs.readSync(descriptor, buffer, 0, buffer.length, 0);
		const header = buffer.subarray(0, length).toString("utf8").split("\n", 1)[0];
		const identity = /^cratis-quality-gate: LOG (\S+) \(cwd: (.*)\)$/.exec(header);
		if (identity) return { gate: identity[1], cwd: identity[2], file: latest.file };
	} catch {
		// Older or unreadable logs keep their existing filename-based display.
	} finally {
		if (descriptor !== undefined) fs.closeSync(descriptor);
	}
	return { gate: latest.gate, file: latest.file };
}

const MAX_LOG_TAIL_BYTES = 64 * 1024;

export function tailLines(file: string, lines: number): string {
	if (lines <= 0) return "";
	let descriptor: number | undefined;
	try {
		descriptor = fs.openSync(file, "r");
		const size = fs.fstatSync(descriptor).size;
		const chunks: Buffer[] = [];
		let position = size;
		let newlines = 0;
		while (position > 0 && size - position < MAX_LOG_TAIL_BYTES && newlines <= lines) {
			const length = Math.min(4096, position, MAX_LOG_TAIL_BYTES - (size - position));
			const chunk = Buffer.allocUnsafe(length);
			const read = fs.readSync(descriptor, chunk, 0, length, position - length);
			if (!read) break;
			position -= read;
			const data = chunk.subarray(0, read);
			chunks.unshift(data);
			for (const byte of data) if (byte === 10) newlines++;
		}
		const text = Buffer.concat(chunks).toString("utf8");
		const tail = text.split("\n").slice(-lines - 1).join("\n").trim();
		return position > 0 && newlines <= lines ? `[earlier log output truncated]\n${tail}` : tail;
	} catch {
		return "";
	} finally {
		if (descriptor !== undefined) fs.closeSync(descriptor);
	}
}

export type Fingerprint = { kind: "ok"; value: string; root: string } | { kind: "not-repository" } | { kind: "unknown"; reason?: string };

/** Digest the working tree; a Git failure or deadline is unknown, never proof that no gate applies. */
export async function workingTreeFingerprint(cwd: string, timeoutMs = FINGERPRINT_TIMEOUT_MS, signal?: AbortSignal): Promise<Fingerprint> {
	const hash = createHash("sha256");
	const deadline = Date.now() + timeoutMs;
	// Read-only: never take the index lock, so this can never make the agent's own git commands fail.
	const env = { ...process.env, GIT_OPTIONAL_LOCKS: "0" };
	const git = async (args: string[], onStdout?: (chunk: Buffer) => void) => {
		if (signal?.aborted || Date.now() >= deadline) return undefined;
		return runBounded("git", args, { cwd, timeoutMs: Math.max(1, deadline - Date.now()), env, signal, onStdout });
	};
	const root = await git(["rev-parse", "--show-toplevel"]);
	if (!root || root.aborted || root.timedOut || root.failed) return { kind: "unknown" };
	if (root.code !== 0) return root.stderr.includes("not a git repository (or any of the parent directories)") ? { kind: "not-repository" } : { kind: "unknown" };
	const status = await git(["status", "--porcelain=v2", "-z", "--untracked-files=all", "--ignore-submodules=none"]);
	if (!status || status.code !== 0 || status.aborted || status.timedOut || status.failed || status.stdoutTruncated) return { kind: "unknown" };
	const head = await git(["rev-parse", "--verify", "-q", "HEAD"]);
	if (!head || head.aborted || head.timedOut || head.failed || (head.code !== 0 && head.code !== 1)) return { kind: "unknown" };
	// Without an initial commit, porcelain status alone does not reflect changes to already-added files.
	if (head.code === 1) return { kind: "unknown", reason: "HEAD has no initial commit" };
	// In porcelain v2 the third field of a tracked entry is Git's submodule state (S<c><m><u>).
	// A dirty gitlink's HEAD and diff can stay identical while its nested content changes.
	// A rename (type 2) has an extra NUL-delimited old path; never parse it as another status record.
	const untracked: string[] = [];
	const entries = status.stdout.split("\0");
	for (let index = 0; index < entries.length; index++) {
		const entry = entries[index];
		if (entry.startsWith("u ")) return { kind: "unknown", reason: "unmerged paths; resolve conflicts before verifying" };
		if (entry.startsWith("? ")) untracked.push(entry.slice(2));
		if (!entry.startsWith("1 ") && !entry.startsWith("2 ")) continue;
		const submoduleState = entry.split(" ", 4)[2];
		if (submoduleState?.startsWith("S") && (submoduleState[2] === "M" || submoduleState[3] === "U")) return { kind: "unknown", reason: "dirty tracked submodule" };
		if (entry.startsWith("2 ")) index++;
	}
	hash.update(`head:${head.stdout.trim()}\0status:${status.stdout}\0`);
	const diff = await git(["diff", "HEAD", "--no-relative", "--no-ext-diff", "--no-textconv", "--binary", "--ignore-submodules=none"], (chunk) => hash.update(chunk));
	if (!diff || diff.code !== 0 || diff.aborted || diff.timedOut || diff.failed) return { kind: "unknown" };
	const top = path.resolve(root.stdout.trim());
	const sameFile = (left: fs.Stats, right: fs.Stats) =>
		left.dev === right.dev && left.ino === right.ino && left.mode === right.mode &&
		left.size === right.size && left.mtimeMs === right.mtimeMs && left.ctimeMs === right.ctimeMs;
	for (const file of untracked) {
		if (signal?.aborted || Date.now() >= deadline) return { kind: "unknown" };
		const parts = file.split("/");
		if (parts.some((part) => !part || part === "." || part === "..")) return { kind: "unknown", reason: `unsupported untracked entry ${JSON.stringify(file)}` };
		const fullPath = path.resolve(top, file);
		const relative = path.relative(top, fullPath);
		if (!relative || relative === ".." || relative.startsWith(`..${path.sep}`) || path.isAbsolute(relative)) return { kind: "unknown" };
		try {
			// Never traverse an untracked symlink directory to read a file outside this repository.
			let directory = top;
			for (const part of parts.slice(0, -1)) {
				if (signal?.aborted || Date.now() >= deadline) return { kind: "unknown" };
				directory = path.join(directory, part);
				const parent = await fs.promises.lstat(directory);
				if (!parent.isDirectory() || parent.isSymbolicLink()) return { kind: "unknown" };
			}
			const before = await fs.promises.lstat(fullPath);
			hash.update(`untracked:${file}:${before.mode}:${before.size}\0`);
			if (before.isSymbolicLink()) {
				// Hash the link text, not the bytes of its target (which may be outside the repository).
				hash.update(`link:${await fs.promises.readlink(fullPath)}\0`);
			} else if (before.isFile() && typeof fs.constants.O_NOFOLLOW === "number") {
				// O_NOFOLLOW and O_NONBLOCK refuse a last-moment symlink or FIFO replacement.
				const handle = await fs.promises.open(fullPath, fs.constants.O_RDONLY | fs.constants.O_NOFOLLOW | fs.constants.O_NONBLOCK);
				try {
					const opened = await handle.stat();
					if (!opened.isFile() || !sameFile(before, opened)) return { kind: "unknown" };
					const buffer = Buffer.allocUnsafe(64 * 1024);
					let offset = 0;
					while (offset < opened.size) {
						if (signal?.aborted || Date.now() >= deadline) return { kind: "unknown" };
						const { bytesRead } = await handle.read(buffer, 0, Math.min(buffer.length, opened.size - offset), offset);
						if (!bytesRead) return { kind: "unknown" };
						hash.update(buffer.subarray(0, bytesRead));
						offset += bytesRead;
					}
					// A file that grew during reading was not fully fingerprinted.
					if ((await handle.stat()).size !== offset) return { kind: "unknown" };
				} finally {
					await handle.close();
				}
				hash.update("\0");
			} else {
				return { kind: "unknown" };
			}
			if (signal?.aborted || Date.now() >= deadline || !sameFile(before, await fs.promises.lstat(fullPath))) return { kind: "unknown" };
		} catch {
			return { kind: "unknown" };
		}
	}
	return signal?.aborted || Date.now() >= deadline ? { kind: "unknown" } : { kind: "ok", value: hash.digest("hex"), root: top };
}
