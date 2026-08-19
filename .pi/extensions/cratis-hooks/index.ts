/**
 * Cratis enforcement hooks for the Pi coding agent.
 *
 * The corpus enforcement scripts under `.ai/hooks/scripts/` are the single source of truth and
 * are wired into Claude Code via `.claude/settings.json`. Pi has no markdown/JSON hook format —
 * lifecycle enforcement is done in an extension — so this bridge subscribes to the equivalent Pi
 * events and drives the SAME scripts, synthesizing the Claude hook JSON they read on stdin:
 *
 *   Claude PreToolUse  (Write|Edit)  →  Pi `tool_call`      →  cratis-guard-writes.sh  (exit 2 = block)
 *   Claude PostToolUse (Write|Edit)  →  Pi `tool_result`    →  cratis-pattern-scan.sh  (advisory context)
 *   Claude Stop                       →  Pi `agent_settled`  →  cratis-quality-gate.sh  (exit 2 = keep going)
 *
 * Nothing here duplicates corpus content: it is adapter machinery, the Pi peer of the Claude
 * `hooks` block in `.claude/settings.json`. Every environment escape hatch the scripts honor
 * (CRATIS_HOOKS_ALLOW_PROTECTED_WRITES, CRATIS_HOOKS_SKIP_SCAN, CRATIS_HOOKS_SKIP_GATE, …) still
 * works because the scripts are executed unchanged.
 */

import { spawn } from "node:child_process";
import * as path from "node:path";
import type { ExtensionAPI } from "@earendil-works/pi-coding-agent";

interface ScriptRun {
	code: number;
	stdout: string;
	stderr: string;
}

/** Run a corpus hook script, feeding `stdinJson` on stdin. Never throws. */
function runScript(script: string, stdinJson: string, cwd: string, signal?: AbortSignal): Promise<ScriptRun> {
	return new Promise<ScriptRun>((resolve) => {
		let proc: ReturnType<typeof spawn>;
		try {
			proc = spawn("bash", [script], { cwd, stdio: ["pipe", "pipe", "pipe"] });
		} catch {
			resolve({ code: 0, stdout: "", stderr: "" });
			return;
		}
		let stdout = "";
		let stderr = "";
		proc.stdout?.on("data", (d) => (stdout += d.toString()));
		proc.stderr?.on("data", (d) => (stderr += d.toString()));
		proc.on("error", () => resolve({ code: 0, stdout, stderr }));
		proc.on("close", (code) => resolve({ code: code ?? 0, stdout, stderr }));
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

export default function (pi: ExtensionAPI) {
	const scriptsDir = path.join(process.cwd(), ".ai", "hooks", "scripts");
	const guardWrites = path.join(scriptsDir, "cratis-guard-writes.sh");
	const patternScan = path.join(scriptsDir, "cratis-pattern-scan.sh");
	const qualityGate = path.join(scriptsDir, "cratis-quality-gate.sh");

	// Mirrors Claude's `stop_hook_active`: true only while the model is continuing because the
	// gate already blocked once this user turn, so the gate never blocks twice in a row (no loop).
	let gateActive = false;
	pi.on("input", async (event) => {
		if (event.source !== "extension") gateActive = false; // a fresh user turn resets the guard
	});

	// ── PreToolUse → guard writes (blocking) ──
	pi.on("tool_call", async (event, ctx) => {
		if (event.toolName !== "write" && event.toolName !== "edit") return;
		const { filePath, content, newString } = writeTarget(event.toolName, (event as any).input);
		if (!filePath) return;
		const payload = JSON.stringify({ cwd: ctx.cwd, tool_input: { file_path: filePath, content, new_string: newString } });
		const run = await runScript(guardWrites, payload, ctx.cwd, ctx.signal);
		if (run.code === 2) return { block: true, reason: run.stderr.trim() || "Blocked by cratis-guard-writes." };
	});

	// ── PostToolUse → deterministic pattern scan (advisory; injects reminders the model sees) ──
	pi.on("tool_result", async (event, ctx) => {
		if (event.toolName !== "write" && event.toolName !== "edit") return;
		if (event.isError) return;
		const { filePath } = writeTarget(event.toolName, (event as any).input);
		if (!filePath) return;
		const payload = JSON.stringify({
			cwd: ctx.cwd,
			session_id: ctx.sessionManager.getSessionId?.() ?? "nosession",
			tool_input: { file_path: filePath },
		});
		const run = await runScript(patternScan, payload, ctx.cwd, ctx.signal);
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
		const payload = JSON.stringify({
			session_id: ctx.sessionManager.getSessionId?.() ?? "nosession",
			stop_hook_active: gateActive,
		});
		const run = await runScript(qualityGate, payload, ctx.cwd);
		if (run.code === 2 && !gateActive) {
			gateActive = true; // don't block again until the next user turn resets it
			const message = run.stderr.trim() || "A Cratis quality gate failed. Fix it and re-run the gate.";
			pi.sendUserMessage(message, { deliverAs: "followUp" });
		}
	});
}
