// cratis-ai-managed: harnesses/pi/extensions/subagent/index.ts
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Cratis Subagent tool for the Pi coding agent.
 *
 * Pi ships no built-in subagents by design — they are an extension pattern. This tool
 * delegates a task to one of the corpus agents (defined once in `.cratis/ai/agents/*.md`, surfaced
 * to Pi via `.pi/agents/*.md` symlinks) by spawning a fresh `pi` subprocess per agent, giving
 * each an isolated context window. It mirrors Pi's official subagent example, trimmed to the
 * essentials and taught to consume the corpus's Claude-shaped agent files through
 * `./agents.ts` (which normalizes tool names and keeps model ids as-is).
 *
 * Modes:
 *   - single:   { agent, task }
 *   - parallel: { tasks: [{ agent, task }, ...] }   (max 8, 4 concurrent)
 *   - chain:    { chain: [{ agent, task }, ...] }    (sequential, {previous} placeholder)
 *
 * Stand-down: when another extension already provides a delegation tool named `Agent` (pi-subagents,
 * which reads the same `.pi/agents/*.md`), this tool removes itself from the session's active tools at
 * session start and says so once — see `./delegation.ts`. Without such a tool it behaves unchanged.
 */

import { spawn } from "node:child_process";
import * as fs from "node:fs";
import * as os from "node:os";
import * as path from "node:path";
import { fileURLToPath } from "node:url";
import { CONFIG_DIR_NAME, type ExtensionAPI, type ExtensionContext, getAgentDir } from "@earendil-works/pi-coding-agent";
import { StringEnum } from "@earendil-works/pi-ai";
import { Type, type Static } from "typebox";
import { type AgentConfig, type AgentScope, discoverAgents } from "./agents.ts";
import { foreignDelegationTool, standDownNotice, SUBAGENT_TOOL_NAME } from "./delegation.ts";
import { readModelTierConfig, resolveModel, type ModelResolution, type ModelTierInputs } from "./modelTiers.ts";

const extensionPath = fileURLToPath(import.meta.url);
const isPackagedExtension = extensionPath.includes(`${path.sep}package${path.sep}corpus${path.sep}`);
const MAX_PARALLEL_TASKS = 8;
const MAX_CONCURRENCY = 4;
const PER_TASK_OUTPUT_CAP = 50 * 1024;

interface SingleResult {
	agent: string;
	agentSource: "package" | "user" | "project" | "unknown";
	task: string;
	exitCode: number;
	finalText: string;
	stderr: string;
	model?: string;
	modelOutcome?: ModelResolution['outcome'];
	modelNotice?: string;
	stopReason?: string;
	errorMessage?: string;
	step?: number;
}

function isFailed(r: SingleResult): boolean {
	return r.exitCode !== 0 || r.stopReason === "error" || r.stopReason === "aborted";
}

function resultOutput(r: SingleResult, results: SingleResult[] = [r]): string {
	const output = isFailed(r) ? r.errorMessage || r.stderr || r.finalText || "(no output)" : r.finalText || "(no output)";
	const notices = [...new Set(results.map(result => result.modelNotice).filter(Boolean))];
	return [...notices, output].join("\n\n");
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

type OutputEvent = { type?: string; message?: { role?: string; content?: { type?: string; text?: string }[]; model?: string; stopReason?: string; errorMessage?: string } };

function assistantMessage(line: string): OutputEvent['message'] {
	let event: OutputEvent;
	try { event = JSON.parse(line); } catch { return undefined; }
	if (event.type !== "message_end") return undefined;
	return event.message?.role === "assistant" ? event.message : undefined;
}

function updateAssistantResult(result: SingleResult, message: NonNullable<OutputEvent['message']>, onText?: (text: string) => void): void {
	for (const part of message.content ?? []) {
		if (part.type === "text" && typeof part.text === "string") {
			result.finalText = part.text;
			onText?.(part.text);
		}
	}
	if (!result.model && message.model) result.model = message.model;
	if (message.stopReason) result.stopReason = message.stopReason;
	if (message.errorMessage) result.errorMessage = message.errorMessage;
}

async function runSingleAgent(
	defaultCwd: string,
	dispatch: Omit<ModelTierInputs, 'agent' | 'dispatchModel'> & { model?: string; thinkingLevel?: string; onNotice: (notice: string) => void },
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

	// A restricted agent whose restriction cannot be expressed in Pi is not launched at all: launching it
	// with the full toolset would be the opposite of what its author declared (see AgentConfig.toolsError).
	if (agent.toolsError) {
		return {
			agent: agentName,
			agentSource: agent.source,
			task,
			exitCode: 1,
			finalText: "",
			stderr: agent.toolsError,
			step,
		};
	}

	const resolution = resolveModel({ ...dispatch, agent, dispatchModel: dispatch.model });
	const { model } = resolution;
	if (resolution.notice) dispatch.onNotice(resolution.notice);
	const args: string[] = ["--mode", "json", "-p", "--no-session"];
	if (model) args.push("--model", model);
	if (resolution.thinkingInherited && dispatch.thinkingLevel) args.push("--thinking", dispatch.thinkingLevel);
	if (agent.tools && agent.tools.length > 0) args.push("--tools", agent.tools.join(","));

	const result: SingleResult = {
		agent: agentName,
		agentSource: agent.source,
		task,
		exitCode: 0,
		finalText: "",
		stderr: "",
		model,
		modelOutcome: resolution.outcome,
		modelNotice: resolution.notice,
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
				const message = assistantMessage(line);
				if (message) updateAssistantResult(result, message, onText);
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
		Type.Boolean({ description: "Set true to prompt before running repo-controlled project agents. Default false.", default: false }),
	),
	cwd: Type.Optional(Type.String({ description: "Working directory for the agent process (single mode)" })),
});

type AgentTask = Static<typeof TaskItem>;
type RunTask = (task: AgentTask, step?: number) => Promise<SingleResult>;

function modelDispatch(ctx: ExtensionContext) {
	let modelNoticeShown = false;
	return {
		...readModelTierConfig(getAgentDir()),
		model: ctx.model ? `${ctx.model.provider}/${ctx.model.id}` : undefined,
		thinkingLevel: ctx.thinkingLevel,
		available: ctx.modelRegistry.getAvailable().map(model => `${model.provider}/${model.id}`),
		onNotice: (notice: string) => {
			if (ctx.hasUI && !modelNoticeShown) {
				modelNoticeShown = true;
				ctx.ui.notify(notice, 'warning');
			}
		},
	};
}

function needsProjectConfirmation(params: Static<typeof SubagentParams>, ctx: ExtensionContext): boolean {
	if (params.agentScope === 'user') return false;
	return (params.confirmProjectAgents ?? false) && Boolean(ctx.hasUI);
}

async function approveProjectAgents(params: Static<typeof SubagentParams>, ctx: ExtensionContext, discovery: ReturnType<typeof discoverAgents>): Promise<boolean> {
	if (!needsProjectConfirmation(params, ctx)) return true;
	const requested = new Set<string>();
	for (const task of params.chain ?? []) requested.add(task.agent);
	for (const task of params.tasks ?? []) requested.add(task.agent);
	if (params.agent) requested.add(params.agent);
	const projectRequested = Array.from(requested)
		.map(name => discovery.agents.find(agent => agent.name === name))
		.filter((agent): agent is AgentConfig => agent?.source === 'project');
	if (projectRequested.length === 0) return true;
	return ctx.ui.confirm(
		"Run project-local agents?",
		`Agents: ${projectRequested.map(agent => agent.name).join(", ")}\nSource: ${discovery.projectAgentsDir ?? "(unknown)"}\n\nProject agents are repo-controlled. Only continue for trusted repositories.`,
	);
}

function exactlyOneMode(params: Static<typeof SubagentParams>): boolean {
	const hasChain = (params.chain?.length ?? 0) > 0;
	const hasTasks = (params.tasks?.length ?? 0) > 0;
	const hasSingle = Boolean(params.agent && params.task);
	return Number(hasChain) + Number(hasTasks) + Number(hasSingle) === 1;
}

async function runChain(chain: AgentTask[], run: RunTask) {
	const results: SingleResult[] = [];
	let previous = "";
	for (let i = 0; i < chain.length; i++) {
		const stepDef = chain[i];
		const task = stepDef.task.replace(/\{previous\}/g, previous);
		const result = await run({ ...stepDef, task }, i + 1);
		results.push(result);
		if (isFailed(result)) {
			return {
				content: [{ type: "text" as const, text: `Chain stopped at step ${i + 1} (${stepDef.agent}): ${resultOutput(result, results)}` }],
				details: { mode: "chain", results },
				isError: true,
			};
		}
		previous = result.finalText;
	}
	return { content: [{ type: "text" as const, text: resultOutput(results[results.length - 1], results) }], details: { mode: "chain", results } };
}

function parallelSummary(result: SingleResult): string {
	const reason = result.stopReason && result.stopReason !== "end" ? ` (${result.stopReason})` : "";
	const status = isFailed(result) ? `failed${reason}` : "completed";
	return `### [${result.agent}] ${status}\n\n${capOutput(resultOutput(result))}`;
}

async function runParallel(tasks: AgentTask[], run: RunTask) {
	if (tasks.length > MAX_PARALLEL_TASKS) {
		return { content: [{ type: "text" as const, text: `Too many parallel tasks (${tasks.length}). Max is ${MAX_PARALLEL_TASKS}.` }], details: {} };
	}
	const results = await mapLimit(tasks, MAX_CONCURRENCY, task => run(task));
	const ok = results.filter(result => !isFailed(result)).length;
	return {
		content: [{ type: "text" as const, text: `Parallel: ${ok}/${results.length} succeeded\n\n${results.map(parallelSummary).join("\n\n---\n\n")}` }],
		details: { mode: "parallel", results },
	};
}

export default function (pi: ExtensionAPI) {
	if (isPackagedExtension && fs.existsSync(path.join(process.cwd(), ".cratis", "ai.manifest.json"))) return;
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
	// Yield to another extension's delegation tool. This can only happen at session start: getAllTools
	// cannot be called while extensions load, and an extension loaded after this one is not visible yet.
	// setActiveTools rebuilds the system prompt before the first turn, so the model never sees this tool.
	let standDownNoticeShown = false;
	pi.on("session_start", (_event, ctx) => {
		try {
			const foreign = foreignDelegationTool(pi.getAllTools());
			if (!foreign) return;
			const active = pi.getActiveTools();
			if (!active.includes(SUBAGENT_TOOL_NAME)) return;
			pi.setActiveTools(active.filter((name) => name !== SUBAGENT_TOOL_NAME));
			if (ctx.hasUI && !standDownNoticeShown) {
				standDownNoticeShown = true;
				ctx.ui.notify(standDownNotice(foreign), "info");
			}
		} catch {
			// Some hosts cannot list or switch tools; being unable to check is no reason to fail the session.
		}
	});

	pi.registerTool({
		name: SUBAGENT_TOOL_NAME,
		label: "Subagent",
		description: [
			"Delegate a task to a specialized Cratis agent in an isolated context (separate pi process).",
			"Modes: single (agent + task), parallel (tasks array, max 8), chain (sequential, {previous} placeholder).",
			`Agents come from the packaged Cratis corpus, ${path.join(CONFIG_DIR_NAME, "agents")} (project), and ${path.join(getAgentDir(), "agents")} (user).`,
			 knownAgents,
		].join(" ").trim(),
		parameters: SubagentParams,

		async execute(_toolCallId, params, signal, _onUpdate, ctx) {
			const agentScope: AgentScope = (params.agentScope as AgentScope) ?? "both";
			const dispatch = modelDispatch(ctx);
			const discovery = discoverAgents(ctx.cwd, agentScope);
			const { agents } = discovery;

			if (!exactlyOneMode(params)) {
				const available = agents.map((a) => `${a.name} (${a.source})`).join(", ") || "none";
				return {
					content: [{ type: "text", text: `Provide exactly one mode (single, parallel, or chain).\nAvailable agents: ${available}` }],
					details: { agents: agents.map((a) => a.name) },
				};
			}

			// Optional security gate for callers that want an extra confirmation for project agents.
			if (!await approveProjectAgents(params, ctx, discovery)) {
				return { content: [{ type: "text", text: "Canceled: project-local agents not approved." }], details: {} };
			}

			const run: RunTask = (task, step) => runSingleAgent(ctx.cwd, dispatch, agents, task.agent, task.task, task.cwd, step, signal, undefined);
			if (params.chain && params.chain.length > 0) return runChain(params.chain, run);
			if (params.tasks && params.tasks.length > 0) return runParallel(params.tasks, run);

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
