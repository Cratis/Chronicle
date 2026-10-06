// cratis-ai-managed: harnesses/pi/extensions/cratis-hooks/index.ts
/**
 * Cratis enforcement hooks for the Pi coding agent.
 *
 * The corpus enforcement scripts under `.cratis/ai/hooks/scripts/` are the single source of truth and
 * are wired into Claude Code via `.claude/settings.json`. Pi has no markdown/JSON hook format —
 * lifecycle enforcement is done in an extension — so this bridge subscribes to the equivalent Pi
 * events and drives the SAME scripts, synthesizing the Claude hook JSON they read on stdin:
 *
 *   Claude PreToolUse  (Write|Edit)  →  Pi `tool_call`      →  cratis-guard-writes.sh  (exit 2 = block)
 *   Claude PreToolUse  (Bash)        →  Pi `tool_call`      →  cratis-guard-store-mutations.sh  (exit 2 = block)
 *   Claude PreToolUse  (Bash)        →  Pi `tool_call`      →  cratis-guard-pr-body.sh  (exit 2 = block)
 *   Claude PostToolUse (Write|Edit)  →  Pi `tool_result`    →  cratis-pattern-scan.sh  (advisory context)
 *   Explicit Pi `cratis_quality_gate` tool → cratis-quality-gate.sh  (exit 2 = failed)
 *
 * The quality gate is the one deliberate difference from Claude. It builds and tests the repository, which can
 * take many minutes, and Pi awaits `agent_settled` handlers while deferring the next prompt — so a gate run there
 * froze the session between prompts, invisibly and with no way to cancel it. Here the gate is an explicit tool
 * the model runs in the foreground: it shows progress, Escape cancels it, it stops at a deadline
 * (CRATIS_HOOKS_GATE_TIMEOUT_SECONDS, default 5 minutes, maximum 10 minutes). A timeout or cancellation
 * is reported as "not verified", never as a pass. It never runs in the background, where it would race the agent's next edits.
 * It is not invoked at every prompt boundary: use affected-project checks while iterating, then run the
 * repository's full CI-equivalent checks before declaring completion or pushing/opening a PR. The explicit tool
 * selects gates for all current working-tree changes; it is not a substitute for the full CI gate.
 *
 * Nothing here duplicates corpus content: it is adapter machinery, the Pi peer of the Claude
 * `hooks` block in `.claude/settings.json`. Every environment escape hatch the scripts honor
 * (CRATIS_HOOKS_ALLOW_PROTECTED_WRITES, CRATIS_HOOKS_ALLOW_STORE_MUTATIONS, CRATIS_HOOKS_SKIP_SCAN,
 * CRATIS_HOOKS_SKIP_GATE, …) still works because the scripts are executed unchanged, in the environment
 * Pi was started from.
 *
 * Only Pi's `bash` tool is bridged to the store-mutation guard. A command the user types directly (`!cmd`,
 * the `user_bash` event) is the person's own action and is deliberately not guarded.
 */

import { spawn } from "node:child_process";
import * as fs from "node:fs";
import * as path from "node:path";
import { fileURLToPath } from "node:url";
import type { ExtensionAPI, ExtensionContext } from "@earendil-works/pi-coding-agent";
import { Type } from "typebox";
import {
	gateLogDirectory,
	gateTimeoutSeconds,
	latestGateLog,
	MAX_GATE_TIMEOUT_SECONDS,
	MIN_GATE_TIMEOUT_SECONDS,
	parseGatePlan,
	PLAN_TIMEOUT_MS,
	FINGERPRINT_TIMEOUT_MS,
	runBounded,
	tailLines,
	workingTreeFingerprint,
} from "./quality-gate.ts";

export const QUALITY_GATE_TOOL_NAME = "cratis_quality_gate";
const HEARTBEAT_MS = 10_000;
const TAIL_LINES = 60;
// runBounded escalates after 5s and settles within another 2s if a child holds its pipes.
const SHUTDOWN_CLEANUP_MS = 8_000;

const extensionPath = fileURLToPath(import.meta.url);
const bundledCorpusRoot = path.resolve(path.dirname(extensionPath), "..", "..", "..", "..");
const isPackagedExtension = extensionPath.includes(`${path.sep}package${path.sep}corpus${path.sep}`);

interface ScriptRun {
	code: number;
	stdout: string;
	stderr: string;
	/** The script could not be executed at all, as opposed to running and deciding. */
	failed?: boolean;
}

/**
 * Run a corpus hook script, feeding `stdinJson` on stdin. Never throws.
 *
 * `failed` separates "the script ran and returned a verdict" from "the script never ran", which
 * the exit code alone cannot express: bash exits 127 for a missing script, and a script that
 * fails to spawn produces no code at all. Both used to surface as `code: 0` — indistinguishable
 * from a deliberate allow — so a hook that was absent or unrunnable silently permitted the very
 * writes it exists to refuse. Callers decide what to do with `failed`; this function only reports
 * it honestly.
 */
function runScript(script: string, stdinJson: string, cwd: string, signal?: AbortSignal): Promise<ScriptRun> {
	return new Promise<ScriptRun>((resolve) => {
		let proc: ReturnType<typeof spawn>;
		try {
			proc = spawn("bash", [script], { cwd, stdio: ["pipe", "pipe", "pipe"] });
		} catch (error) {
			resolve({ code: 127, stdout: "", stderr: String(error), failed: true });
			return;
		}
		let stdout = "";
		let stderr = "";
		proc.stdout?.on("data", (d) => (stdout += d.toString()));
		proc.stderr?.on("data", (d) => (stderr += d.toString()));
		proc.on("error", (error) => resolve({ code: 127, stdout, stderr: stderr || String(error), failed: true }));
		proc.on("close", (code) => resolve({ code: code ?? 0, stdout, stderr, failed: code === 127 }));
		if (signal) {
			const kill = () => proc.kill("SIGTERM");
			if (signal.aborted) kill();
			else signal.addEventListener("abort", kill, { once: true });
		}
		// A script that exits before reading stdin (or doesn't read it at all) closes its end of the
		// pipe first; the write then fails with EPIPE asynchronously, after this try/catch has already
		// returned. Without a listener here that surfaces as an unhandled 'error' event and crashes the
		// process, even though the exit code the close handler already captured is the real verdict.
		proc.stdin?.on("error", () => {
			/* the child may exit before (or without) reading stdin; its exit code is still the verdict */
		});
		try {
			proc.stdin?.write(stdinJson);
			proc.stdin?.end();
		} catch {
			/* ignore */
		}
	});
}

/** file_path + written content, extracted from Pi's write/edit tool inputs. */
function writeTarget(toolName: string, input: any): { filePath?: string; content?: string; newString?: string } {
	const filePath = input?.path ?? input?.file_path;
	if (toolName === "write") return { filePath, content: typeof input?.content === "string" ? input.content : undefined };
	if (toolName === "edit") {
		const edits = Array.isArray(input?.edits) ? input.edits : [];
		const newString = edits.map((e: any) => (typeof e?.newText === "string" ? e.newText : "")).join("\n");
		return { filePath, newString: newString || undefined };
	}
	return { filePath };
}

type ToolCallContext = { cwd: string; signal?: AbortSignal };

/**
 * Run a blocking PreToolUse guard and translate its verdict into Pi's `tool_call` result.
 *
 * Exit 2 blocks with the script's stderr as the reason. A guard that is installed but could not run blocks too:
 * allowing there would let the one case the guard exists to catch pass silently precisely because the guard
 * is broken, so a broken guard is loud rather than permissive.
 */
async function runBlockingGuard(
	script: string,
	name: string,
	subject: string,
	payload: object,
	ctx: ToolCallContext,
	fixTarget = "this guard",
): Promise<{ block: true; reason: string } | undefined> {
	const run = await runScript(script, JSON.stringify(payload), ctx.cwd, ctx.signal);
	if (run.failed) {
		return {
			block: true,
			reason:
				`${name} is installed at ${script} but could not be run, so this ${subject} cannot be checked.` +
				`${run.stderr.trim() ? `\n\n${run.stderr.trim()}` : ""}` +
				`\n\nFix the script (or remove it if this repository is not meant to enforce ${fixTarget}) and retry.`,
		};
	}
	if (run.code === 2) return { block: true, reason: run.stderr.trim() || `Blocked by ${name}.` };
	return undefined;
}

/**
 * Whether a hook script is installed at all.
 *
 * A repository that ships no hook scripts is a supported configuration — the corpus scripts
 * themselves degrade to a silent no-op when `jq` is missing, on the stated principle that a hook
 * must never break a session. So an absent script is not an error and is not enforced.
 *
 * The dangerous case is the other one: the script is present, so this repository clearly intends
 * the guard to run, but it cannot be executed. That is a broken guard rather than an absent one,
 * and it is the case that must never be mistaken for permission.
 */
function isInstalled(script: string): boolean {
	try {
		return fs.statSync(script).isFile();
	} catch {
		return false;
	}
}

export default function (pi: ExtensionAPI) {
	if (isPackagedExtension && fs.existsSync(path.join(process.cwd(), ".cratis", "ai.manifest.json"))) return;
	const managedScriptsDir = path.join(process.cwd(), ".cratis", "ai", "hooks", "scripts");
	const scriptsDir = fs.existsSync(managedScriptsDir) ? managedScriptsDir : path.join(bundledCorpusRoot, "hooks", "scripts");
	const guardWrites = path.join(scriptsDir, "cratis-guard-writes.sh");
	const guardStoreMutations = path.join(scriptsDir, "cratis-guard-store-mutations.sh");
	const guardPrBody = path.join(scriptsDir, "cratis-guard-pr-body.sh");
	const patternScan = path.join(scriptsDir, "cratis-pattern-scan.sh");
	const qualityGate = path.join(scriptsDir, "cratis-quality-gate.sh");

	// The explicit gate owns its subprocesses; shutdown also cancels a still-running tool.
	const activeGateRuns = new Map<AbortController, Promise<void>>();
	pi.on("session_shutdown", async () => {
		const runs = [...activeGateRuns];
		for (const [controller] of runs) controller.abort();
		// Pi exits when shutdown handlers return. Let cancelled runs escalate to SIGKILL first.
		let timer: NodeJS.Timeout | undefined;
		try {
			await Promise.race([
				Promise.allSettled(runs.map(([, cleanup]) => cleanup)),
				new Promise<void>((resolve) => { timer = setTimeout(resolve, SHUTDOWN_CLEANUP_MS); }),
			]);
		} finally {
			if (timer) clearTimeout(timer);
		}
	});

	// ── PreToolUse → guard writes and store-mutating cratis commands (blocking) ──
	pi.on("tool_call", async (event, ctx) => {
		if (event.toolName === "bash") {
			const command = (event as any).input?.command;
			if (typeof command !== "string" || !command.trim()) return;
			const payload = { cwd: ctx.cwd, tool_name: "Bash", tool_input: { command } };
			if (isInstalled(guardStoreMutations)) {
				const blocked = await runBlockingGuard(guardStoreMutations, "cratis-guard-store-mutations", "command", payload, ctx);
				if (blocked) return blocked;
			}
			if (isInstalled(guardPrBody)) {
				const run = await runScript(guardPrBody, JSON.stringify(payload), ctx.cwd, ctx.signal);
				if (run.failed || run.code !== 0) return { block: true, reason: run.stderr.trim() || "cratis-guard-pr-body could not check this command." };
				if (run.stderr.trim()) pi.sendMessage({ customType: "cratis-pr-body-warning", content: run.stderr.trim(), display: true });
			}
			return;
		}
		if (event.toolName !== "write" && event.toolName !== "edit") return;
		const { filePath, content, newString } = writeTarget(event.toolName, (event as any).input);
		if (!filePath) return;
		if (!isInstalled(guardWrites)) return; // no guard installed in this repository - nothing to enforce
		const payload = { cwd: ctx.cwd, tool_input: { file_path: filePath, content, new_string: newString } };
		return runBlockingGuard(guardWrites, "cratis-guard-writes", "write", payload, ctx, "write guards");
	});

	// ── PostToolUse → deterministic pattern scan (advisory; injects reminders the model sees) ──
	pi.on("tool_result", async (event, ctx) => {
		if (event.toolName !== "write" && event.toolName !== "edit") return;
		if (event.isError) return;
		const { filePath } = writeTarget(event.toolName, (event as any).input);
		if (!filePath) return;
		if (!isInstalled(patternScan)) return;
		const payload = JSON.stringify({
			cwd: ctx.cwd,
			session_id: ctx.sessionManager.getSessionId?.() ?? "nosession",
			tool_input: { file_path: filePath },
		});
		const run = await runScript(patternScan, payload, ctx.cwd, ctx.signal);

		// Advisory, not a gate: a broken pattern scan must not fail a write that already succeeded.
		// It is still surfaced rather than swallowed, because a scan that silently stops running
		// looks exactly like a scan that finds nothing.
		if (run.failed) {
			const existing = Array.isArray(event.content) ? event.content : [];
			return {
				content: [
					...existing,
					{
						type: "text",
						text: `\n\n[cratis-hooks] cratis-pattern-scan is installed but could not be run, so no pattern checks were applied to this edit.`,
					},
				],
			};
		}
		if (run.code !== 0 || !run.stdout.trim()) return;
		let reminder = "";
		try {
			reminder = JSON.parse(run.stdout)?.hookSpecificOutput?.additionalContext ?? "";
		} catch {
			reminder = "";
		}
		if (!reminder.trim()) return;
		const existing = Array.isArray(event.content) ? event.content : [];
		return { content: [...existing, { type: "text", text: `\n\n[cratis-hooks]\n${reminder.trim()}` }] };
	});

	// The quality gate is only run by its explicit tool, never by a turn-end handler.
	const sessionIdOf = (ctx: ExtensionContext) => ctx.sessionManager.getSessionId?.() ?? "nosession";
	const gatePayload = (sessionId: string) => JSON.stringify({ session_id: sessionId, stop_hook_active: false });

	/** The gates the current changes would run, from the script's own dry run. Bounded; never runs a gate. */
	async function planGates(ctx: ExtensionContext, root: string, signal?: AbortSignal): Promise<{ gates: string[]; problem?: string }> {
		const run = await runBounded("bash", [qualityGate], {
			cwd: ctx.cwd,
			stdin: gatePayload(sessionIdOf(ctx)),
			timeoutMs: PLAN_TIMEOUT_MS,
			signal,
			env: { ...process.env, CLAUDE_PROJECT_DIR: root, CRATIS_HOOKS_GATE_DRYRUN: "1" },
		});
		const gates = parseGatePlan(run.stderr);
		if (run.aborted) return { gates: [], problem: "its dry run was cancelled" };
		if (run.timedOut) return { gates, problem: `its dry run did not finish within ${PLAN_TIMEOUT_MS / 1000}s` };
		if (run.failed || run.code !== 0) return { gates, problem: `its dry run could not be completed (exit ${run.code ?? "none"})` };
		return { gates };
	}

	// ── The quality gate itself: explicit, foreground, bounded, cancellable ──
	pi.registerTool({
		name: QUALITY_GATE_TOOL_NAME,
		label: "Cratis quality gate",
		description:
			"Run the Cratis quality gate (.cratis/ai/hooks/scripts/cratis-quality-gate.sh): the build, spec and lint gates the current " +
			"working-tree changes touch, configured in quality-gates.json. Runs in the foreground with a deadline and can be cancelled. " +
			"A failure, a timeout or a cancellation is returned as an error and means the change is NOT verified. " +
			"Run it explicitly at a verification checkpoint, not after every prompt. It selects gates for all current " +
			"working-tree changes, not the complete CI matrix; run CI-equivalent gates before claiming completion or pushing/opening a PR. " +
			"Tracked-file stability is Git-visible, not byte-exact: lossy clean/EOL conversion and assume-unchanged/skip-worktree " +
			"can hide raw disk changes. Verify raw content independently or remove those settings before relying on this check.",
		promptSnippet: "Run the Cratis quality gate for the current changes (bounded, cancellable)",
		parameters: Type.Object({
			timeoutSeconds: Type.Optional(
				Type.Integer({
					minimum: MIN_GATE_TIMEOUT_SECONDS,
					maximum: MAX_GATE_TIMEOUT_SECONDS,
					description:
						"Stop the gate after this many seconds (default 300; maximum 600). Raise it only for a concrete reason within the authorized execution budget.",
				}),
			),
		}),
		executionMode: "sequential",

		async execute(_toolCallId, params, signal, onUpdate, ctx) {
			if (!isInstalled(qualityGate)) {
				return { content: [{ type: "text", text: `No Cratis quality gate is installed (${qualityGate}), so nothing was checked.` }], details: { status: "not-installed" } };
			}
			if (process.env.CRATIS_HOOKS_SKIP_GATE === "1") {
				return { content: [{ type: "text", text: "CRATIS_HOOKS_SKIP_GATE=1 is set, so the Cratis quality gate was skipped and nothing was verified." }], details: { status: "skipped" } };
			}
			if (process.env.CRATIS_HOOKS_GATE_DRYRUN === "1") {
				return { content: [{ type: "text", text: "CRATIS_HOOKS_GATE_DRYRUN=1 is set: this is a dry run, NOT VERIFIED. No gates were executed." }], details: { status: "dry-run" } };
			}

			const timeoutSeconds = gateTimeoutSeconds(params.timeoutSeconds);
			const sessionId = sessionIdOf(ctx);
			const started = Date.now();
			const deadline = started + timeoutSeconds * 1000;
			const cancel = new AbortController();
			const expired = new AbortController();
			const deadlineTimer = setTimeout(() => expired.abort(), timeoutSeconds * 1000);
			const operationSignal = AbortSignal.any([...(signal ? [signal] : []), cancel.signal, expired.signal]);
			let completed!: () => void;
			const cleanup = new Promise<void>((resolve) => { completed = resolve; });
			activeGateRuns.set(cancel, cleanup);
			try {
			const reportTimedOut = () => {
				const last = latestGateLog(gateLogDirectory(sessionId), started);
				const tail = last ? tailLines(last.file, TAIL_LINES) : "";
				throw new Error(`The Cratis quality gate TIMED OUT after ${duration(timeoutSeconds * 1000)}${last ? ` while running '${last.gate}'` : ""}. This is not a pass: nothing was verified.` +
					(last ? `\n\nGate log: ${last.file}` : "") +
					(tail ? `\n\n--- last ${TAIL_LINES} lines ---\n${tail}\n--- end ---` : ""));
			};
			const reportInterrupted = () => {
				if (expired.signal.aborted) reportTimedOut();
				if (operationSignal.aborted) throw new Error("The Cratis quality gate was cancelled. Nothing was verified.");
			};
			const remaining = () => Math.max(1, deadline - Date.now());
			const logDirectory = gateLogDirectory(sessionId);
			onUpdate?.({ content: [{ type: "text", text: `Planning Cratis quality gate (${duration(timeoutSeconds * 1000)} deadline). Escape cancels.` }], details: { status: "planning", timeoutSeconds } });
			reportInterrupted();
			const before = await workingTreeFingerprint(ctx.cwd, Math.min(FINGERPRINT_TIMEOUT_MS, remaining()), operationSignal);
			reportInterrupted();
			if (before.kind === "not-repository") throw new Error("The Cratis quality gate is not in a Git repository; nothing was verified.");
			if (before.kind === "unknown") throw new Error(`The working tree could not be checked before the Cratis quality gate${before.reason ? `: ${before.reason}` : ""}. Nothing was verified.`);
			const plan = await planGates(ctx, before.root, operationSignal);
			reportInterrupted();
			if (plan.problem) throw new Error(`The Cratis quality gate plan could not be completed: ${plan.problem}. Nothing was verified.`);
			const planned = plan.gates.length > 0 ? plan.gates.join(", ") : "none planned";
			const progress = (elapsedMs: number) => {
				const current = latestGateLog(logDirectory, started)?.gate;
				onUpdate?.({
					content: [
						{
							type: "text",
							text: `Cratis quality gate running${current ? ` ${current}` : ""}: ${duration(elapsedMs)} of ${duration(timeoutSeconds * 1000)} (gates: ${planned}). Escape cancels.`,
						},
					],
					details: { status: "running", gates: plan.gates, current, elapsedMs, timeoutSeconds },
				});
			};
			progress(Date.now() - started);

			const run = await runBounded("bash", [qualityGate], {
				cwd: ctx.cwd,
				stdin: gatePayload(sessionId),
				timeoutMs: remaining(),
				signal: operationSignal,
				env: { ...process.env, CLAUDE_PROJECT_DIR: before.root },
				onHeartbeat: () => progress(Date.now() - started),
				heartbeatMs: HEARTBEAT_MS,
			});
			// Cancellation never starts another Git process. Only a successful gate needs a post-run fingerprint.
			if (run.aborted || operationSignal.aborted) reportInterrupted();
			if (run.timedOut) reportTimedOut();
			const notes = run.stderr.trim() ? `\n\n${run.stderr.trim()}` : "";
			if (run.failed) {
				throw new Error(`The Cratis quality gate is installed at ${qualityGate} but could not be run, so nothing was verified.${notes}`);
			}
			if (run.code === 2) throw new Error(run.stderr.trim() || "A Cratis quality gate failed. Fix it and re-run the gate.");
			if (run.code !== 0) throw new Error(`The Cratis quality gate exited unexpectedly with code ${run.code ?? "none"}; treat the change as not verified.${notes}`);
			const after = await workingTreeFingerprint(ctx.cwd, Math.min(FINGERPRINT_TIMEOUT_MS, remaining()), operationSignal);
			reportInterrupted();
			if (before.kind !== "ok" || after.kind !== "ok" || before.value !== after.value) {
				throw new Error("The working tree changed while the Cratis quality gate ran (or could not be checked afterward). The gate tested an older tree; this tree is NOT VERIFIED. Re-run after changes settle.");
			}
			const summary =
				plan.gates.length > 0
					? `The Cratis quality gate passed in ${duration(run.durationMs)} (gates: ${planned}).`
					: "No Cratis quality gate was planned for the current changes; nothing was verified by this tool.";
			return { content: [{ type: "text", text: `${summary}${notes}` }], details: { status: plan.gates.length > 0 ? "passed" : "no-applicable-gates", gates: plan.gates, durationMs: run.durationMs } };
			} finally {
				clearTimeout(deadlineTimer);
				activeGateRuns.delete(cancel);
				completed();
			}
		},
	});
}

function duration(ms: number): string {
	const seconds = Math.round(ms / 1000);
	return seconds < 60 ? `${seconds}s` : `${Math.floor(seconds / 60)}m${String(seconds % 60).padStart(2, "0")}s`;
}
