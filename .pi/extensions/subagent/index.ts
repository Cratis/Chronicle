/**
 * Cratis Subagent tool for the Pi coding agent.
 *
 * Pi ships no built-in subagents by design — they are an extension pattern. This tool
 * delegates a task to one of the corpus agents (defined once in `.ai/agents/*.md`, surfaced
 * to Pi via `.pi/agents/*.md` symlinks) by spawning a fresh `pi` subprocess per agent, giving
 * each an isolated context window. It mirrors Pi's official subagent example, trimmed to the
 * essentials and taught to consume the corpus's Claude-shaped agent files through
 * `./agents.ts` (which normalizes tool names and keeps model ids as-is).
 *
 * Modes:
 *   - single:   { agent, task }
 *   - parallel: { tasks: [{ agent, task }, ...] }   (max 8, 4 concurrent)
 *   - chain:    { chain: [{ agent, task }, ...] }    (sequential, {previous} placeholder)
 */

import { spawn } from "node:child_process";
import * as fs from "node:fs";
import * as os from "node:os";
import * as path from "node:path";
import { CONFIG_DIR_NAME, type ExtensionAPI, getAgentDir } from "@earendil-works/pi-coding-agent";
import { StringEnum } from "@earendil-works/pi-ai";
import { Type } from "typebox";
import { type AgentConfig, type AgentScope, discoverAgents } from "./agents.ts";

const MAX_PARALLEL_TASKS = 8;
const MAX_CONCURRENCY = 4;
const PER_TASK_OUTPUT_CAP = 50 * 1024;

interface SingleResult {
	agent: string;
	agentSource: "user" | "project" | "unknown";
	task: string;
	exitCode: number;
	finalText: string;
	stderr: string;
	model?: string;
	stopReason?: string;
	errorMessage?: string;
	step?: number;
}

function isFailed(r: SingleResult): boolean {
	return r.exitCode !== 0 || r.stopReason === "error" || r.stopReason === "aborted";
}

function resultOutput(r: SingleResult): string {
	if (isFailed(r)) return r.errorMessage || r.stderr || r.finalText || "(no output)";
	return r.finalText || "(no output)";
}

function capOutput(output: string): string {
	if (Buffer.byteLength(output, "utf8") <= PER_TASK_OUTPUT_CAP) return output;
	let truncated = output.slice(0, PER_TASK_OUTPUT_CAP);
	while (Buffer.byteLength(truncated, "utf8") > PER_TASK_OUTPUT_CAP) truncated = truncated.slice(0, -1);
	return `${truncated}\n\n[Output truncated. Full output preserved in tool details.]`;
}

/** Resolve how to re-invoke this same Pi build for a child process. */
function piInvocation(args: string[]): { command: string; args: string[] } {
	const currentScript = process.argv[1];
	const isBunVirtual = currentScript?.startsWith("/$bunfs/root/");
	if (currentScript && !isBunVirtual && fs.existsSync(currentScript)) {
		return { command: process.execPath, args: [currentScript, ...args] };
	}
	const execName = path.basename(process.execPath).toLowerCase();
	if (!/^(node|bun)(\.exe)?$/.test(execName)) return { command: process.execPath, args };
	return { command: "pi", args };
}

async function runSingleAgent(
	defaultCwd: string,
	dispatch: { model?: string; thinkingLevel?: string },
	agents: AgentConfig[],
	agentName: string,
	task: string,
	cwd: string | undefined,
	step: number | undefined,
	signal: AbortSignal | undefined,
	onText: ((text: string) => void) | undefined,
): Promise<SingleResult> {
	const agent = agents.find((a) => a.name === agentName);
	if (!agent) {
		const available = agents.map((a) => `"${a.name}"`).join(", ") || "none";
		return {
			agent: agentName,
			agentSource: "unknown",
			task,
			exitCode: 1,
			finalText: "",
			stderr: `Unknown agent: "${agentName}". Available agents: ${available}.`,
			step,
		};
	}

	// An agent that pins no model inherits the dispatching session's model + thinking level.
	const model = agent.model ?? dispatch.model;
	const args: string[] = ["--mode", "json", "-p", "--no-session"];
	if (model) args.push("--model", model);
	if (!agent.model && dispatch.thinkingLevel) args.push("--thinking", dispatch.thinkingLevel);
	if (agent.tools && agent.tools.length > 0) args.push("--tools", agent.tools.join(","));

	const result: SingleResult = {
		agent: agentName,
		agentSource: agent.source,
		task,
		exitCode: 0,
		finalText: "",
		stderr: "",
		model,
		step,
	};

	let tmpDir: string | null = null;
	let tmpFile: string | null = null;
	try {
		if (agent.systemPrompt.trim()) {
			tmpDir = await fs.promises.mkdtemp(path.join(os.tmpdir(), "pi-subagent-"));
			tmpFile = path.join(tmpDir, `prompt-${agent.name.replace(/[^\w.-]+/g, "_")}.md`);
			await fs.promises.writeFile(tmpFile, agent.systemPrompt, { encoding: "utf-8", mode: 0o600 });
			args.push("--append-system-prompt", tmpFile);
		}
		args.push(`Task: ${task}`);

		let aborted = false;
		const exitCode = await new Promise<number>((resolve) => {
			const inv = piInvocation(args);
			const proc = spawn(inv.command, inv.args, {
				cwd: cwd ?? defaultCwd,
				shell: false,
				stdio: ["ignore", "pipe", "pipe"],
			});
			let buffer = "";
			const processLine = (line: string) => {
				if (!line.trim()) return;
				let event: any;
				try {
					event = JSON.parse(line);
				} catch {
					return;
				}
				if (event.type === "message_end" && event.message?.role === "assistant") {
					const msg = event.message;
					for (const part of msg.content ?? []) {
						if (part.type === "text" && typeof part.text === "string") {
							result.finalText = part.text;
							onText?.(part.text);
						}
					}
					if (!result.model && msg.model) result.model = msg.model;
					if (msg.stopReason) result.stopReason = msg.stopReason;
					if (msg.errorMessage) result.errorMessage = msg.errorMessage;
				}
			};
			proc.stdout.on("data", (data) => {
				buffer += data.toString();
				const lines = buffer.split("\n");
				buffer = lines.pop() || "";
				for (const line of lines) processLine(line);
			});
			proc.stderr.on("data", (data) => {
				result.stderr += data.toString();
			});
			proc.on("close", (code) => {
				if (buffer.trim()) processLine(buffer);
				resolve(code ?? 0);
			});
			proc.on("error", () => resolve(1));
			if (signal) {
				const kill = () => {
					aborted = true;
					proc.kill("SIGTERM");
					setTimeout(() => {
						if (!proc.killed) proc.kill("SIGKILL");
					}, 5000);
				};
				if (signal.aborted) kill();
				else signal.addEventListener("abort", kill, { once: true });
			}
		});

		result.exitCode = exitCode;
		if (aborted) result.stopReason = "aborted";
		return result;
	} finally {
		if (tmpFile) try { fs.unlinkSync(tmpFile); } catch { /* ignore */ }
		if (tmpDir) try { fs.rmdirSync(tmpDir); } catch { /* ignore */ }
	}
}

async function mapLimit<TIn, TOut>(items: TIn[], limit: number, fn: (item: TIn, i: number) => Promise<TOut>): Promise<TOut[]> {
	if (items.length === 0) return [];
	const width = Math.max(1, Math.min(limit, items.length));
	const results: TOut[] = new Array(items.length);
	let next = 0;
	await Promise.all(
		new Array(width).fill(null).map(async () => {
			while (true) {
				const i = next++;
				if (i >= items.length) return;
				results[i] = await fn(items[i], i);
			}
		}),
	);
	return results;
}

const TaskItem = Type.Object({
	agent: Type.String({ description: "Name of the agent to invoke" }),
	task: Type.String({ description: "Task to delegate to the agent" }),
	cwd: Type.Optional(Type.String({ description: "Working directory for the agent process" })),
});

const SubagentParams = Type.Object({
	agent: Type.Optional(Type.String({ description: "Agent name (single mode)" })),
	task: Type.Optional(Type.String({ description: "Task to delegate (single mode)" })),
	tasks: Type.Optional(Type.Array(TaskItem, { description: "{agent, task} items to run in parallel" })),
	chain: Type.Optional(Type.Array(TaskItem, { description: "{agent, task} items run in order; use {previous} for prior output" })),
	agentScope: Type.Optional(
		StringEnum(["user", "project", "both"] as const, {
			description: 'Which agent dirs to use. Default "both" (project .pi/agents override ~/.pi/agent/agents).',
			default: "both",
		}),
	),
	confirmProjectAgents: Type.Optional(
		Type.Boolean({ description: "Prompt before running repo-controlled project agents. Default true.", default: true }),
	),
	cwd: Type.Optional(Type.String({ description: "Working directory for the agent process (single mode)" })),
});

export default function (pi: ExtensionAPI) {
	// Discover once at registration so the tool description can name the valid agents — the model
	// otherwise has to guess an `agent` value. Per-call execution rediscovers, so editing an agent
	// mid-session still takes effect; only this hint list is fixed until the next /reload.
	let knownAgents = "";
	try {
		const names = discoverAgents(process.cwd(), "both").agents.map((a) => a.name);
		if (names.length > 0) knownAgents = ` Available agents: ${names.map((n) => `"${n}"`).join(", ")}.`;
	} catch {
		knownAgents = "";
	}
	pi.registerTool({
		name: "subagent",
		label: "Subagent",
		description: [
			"Delegate a task to a specialized Cratis agent in an isolated context (separate pi process).",
			"Modes: single (agent + task), parallel (tasks array, max 8), chain (sequential, {previous} placeholder).",
			`Agents come from ${path.join(CONFIG_DIR_NAME, "agents")} (project) and ${path.join(getAgentDir(), "agents")} (user).`,
			 knownAgents,
		].join(" ").trim(),
		parameters: SubagentParams,

		async execute(_toolCallId, params, signal, _onUpdate, ctx) {
			const agentScope: AgentScope = (params.agentScope as AgentScope) ?? "both";
			const dispatch = {
				model: ctx.model ? `${ctx.model.provider}/${ctx.model.id}` : undefined,
				thinkingLevel: ctx.thinkingLevel,
			};
			const { agents, projectAgentsDir } = discoverAgents(ctx.cwd, agentScope);

			const hasChain = (params.chain?.length ?? 0) > 0;
			const hasTasks = (params.tasks?.length ?? 0) > 0;
			const hasSingle = Boolean(params.agent && params.task);
			if (Number(hasChain) + Number(hasTasks) + Number(hasSingle) !== 1) {
				const available = agents.map((a) => `${a.name} (${a.source})`).join(", ") || "none";
				return {
					content: [{ type: "text", text: `Provide exactly one mode (single, parallel, or chain).\nAvailable agents: ${available}` }],
					details: { agents: agents.map((a) => a.name) },
				};
			}

			// Security gate: project agents are repo-controlled prompts.
			if ((agentScope === "project" || agentScope === "both") && (params.confirmProjectAgents ?? true) && ctx.hasUI) {
				const requested = new Set<string>();
				for (const s of params.chain ?? []) requested.add(s.agent);
				for (const t of params.tasks ?? []) requested.add(t.agent);
				if (params.agent) requested.add(params.agent);
				const projectRequested = Array.from(requested)
					.map((n) => agents.find((a) => a.name === n))
					.filter((a): a is AgentConfig => a?.source === "project");
				if (projectRequested.length > 0) {
					const ok = await ctx.ui.confirm(
						"Run project-local agents?",
						`Agents: ${projectRequested.map((a) => a.name).join(", ")}\nSource: ${projectAgentsDir ?? "(unknown)"}\n\nProject agents are repo-controlled. Only continue for trusted repositories.`,
					);
					if (!ok) return { content: [{ type: "text", text: "Canceled: project-local agents not approved." }], details: {} };
				}
			}

			// ── chain ──
			if (params.chain && params.chain.length > 0) {
				const results: SingleResult[] = [];
				let previous = "";
				for (let i = 0; i < params.chain.length; i++) {
					const stepDef = params.chain[i];
					const task = stepDef.task.replace(/\{previous\}/g, previous);
					const r = await runSingleAgent(ctx.cwd, dispatch, agents, stepDef.agent, task, stepDef.cwd, i + 1, signal, undefined);
					results.push(r);
					if (isFailed(r)) {
						return {
							content: [{ type: "text", text: `Chain stopped at step ${i + 1} (${stepDef.agent}): ${resultOutput(r)}` }],
							details: { mode: "chain", results },
							isError: true,
						};
					}
					previous = r.finalText;
				}
				return {
					content: [{ type: "text", text: resultOutput(results[results.length - 1]) }],
					details: { mode: "chain", results },
				};
			}

			// ── parallel ──
			if (params.tasks && params.tasks.length > 0) {
				if (params.tasks.length > MAX_PARALLEL_TASKS) {
					return {
						content: [{ type: "text", text: `Too many parallel tasks (${params.tasks.length}). Max is ${MAX_PARALLEL_TASKS}.` }],
						details: {},
					};
				}
				const results = await mapLimit(params.tasks, MAX_CONCURRENCY, (t) =>
					runSingleAgent(ctx.cwd, dispatch, agents, t.agent, t.task, t.cwd, undefined, signal, undefined),
				);
				const ok = results.filter((r) => !isFailed(r)).length;
				const summaries = results.map((r) => {
					const status = isFailed(r) ? `failed${r.stopReason && r.stopReason !== "end" ? ` (${r.stopReason})` : ""}` : "completed";
					return `### [${r.agent}] ${status}\n\n${capOutput(resultOutput(r))}`;
				});
				return {
					content: [{ type: "text", text: `Parallel: ${ok}/${results.length} succeeded\n\n${summaries.join("\n\n---\n\n")}` }],
					details: { mode: "parallel", results },
				};
			}

			// ── single ──
			const r = await runSingleAgent(ctx.cwd, dispatch, agents, params.agent!, params.task!, params.cwd, undefined, signal, undefined);
			if (isFailed(r)) {
				return {
					content: [{ type: "text", text: `Agent ${r.stopReason || "failed"}: ${resultOutput(r)}` }],
					details: { mode: "single", results: [r] },
					isError: true,
				};
			}
			return { content: [{ type: "text", text: resultOutput(r) }], details: { mode: "single", results: [r] } };
		},
	});
}
