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
 *   Claude PostToolUse (Write|Edit)  →  Pi `tool_result`    →  cratis-pattern-scan.sh  (advisory context)
 *   Claude Stop                       →  Pi `agent_settled`  →  cratis-quality-gate.sh  (exit 2 = keep going)
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
import type { ExtensionAPI } from "@earendil-works/pi-coding-agent";

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
	const patternScan = path.join(scriptsDir, "cratis-pattern-scan.sh");
	const qualityGate = path.join(scriptsDir, "cratis-quality-gate.sh");

	// Mirrors Claude's `stop_hook_active`: true only while the model is continuing because the
	// gate already blocked once this user turn, so the gate never blocks twice in a row (no loop).
	let gateActive = false;
	pi.on("input", async (event) => {
		if (event.source !== "extension") gateActive = false; // a fresh user turn resets the guard
	});

	// ── PreToolUse → guard writes and store-mutating cratis commands (blocking) ──
	pi.on("tool_call", async (event, ctx) => {
		if (event.toolName === "bash") {
			const command = (event as any).input?.command;
			if (typeof command !== "string" || !command.trim()) return;
			if (!isInstalled(guardStoreMutations)) return; // no guard installed in this repository - nothing to enforce
			const payload = { cwd: ctx.cwd, tool_name: "Bash", tool_input: { command } };
			return runBlockingGuard(guardStoreMutations, "cratis-guard-store-mutations", "command", payload, ctx);
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

	// ── Stop → quality gate (re-runs the gates the change touched; keeps the model going on failure) ──
	pi.on("agent_settled", async (_event, ctx) => {
		if (ctx.mode === "print" || ctx.mode === "json") return;
		if (!isInstalled(qualityGate)) return;
		const payload = JSON.stringify({
			session_id: ctx.sessionManager.getSessionId?.() ?? "nosession",
			stop_hook_active: gateActive,
		});
		const run = await runScript(qualityGate, payload, ctx.cwd);

		// A gate that cannot run has not passed. Say so rather than letting the turn end quietly,
		// but only once per user turn, on the same re-entry guard a real gate failure uses.
		if (run.failed && !gateActive) {
			gateActive = true;
			pi.sendUserMessage(
				`The Cratis quality gate is installed at ${qualityGate} but could not be run, so nothing was verified this turn.` +
					`${run.stderr.trim() ? `\n\n${run.stderr.trim()}` : ""}`,
				{ deliverAs: "followUp" },
			);
			return;
		}
		if (run.code === 2 && !gateActive) {
			gateActive = true; // don't block again until the next user turn resets it
			const message = run.stderr.trim() || "A Cratis quality gate failed. Fix it and re-run the gate.";
			pi.sendUserMessage(message, { deliverAs: "followUp" });
		}
	});
}
