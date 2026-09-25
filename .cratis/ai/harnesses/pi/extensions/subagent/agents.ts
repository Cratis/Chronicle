// cratis-ai-managed: harnesses/pi/extensions/subagent/agents.ts
/**
 * Agent discovery + format normalization for the Cratis subagent tool.
 *
 * The agent definitions are the SINGLE-SOURCE corpus files under `.cratis/ai/agents/*.md`,
 * surfaced to Pi through symlink adapters in `.pi/agents/*.md`. Those files are written
 * in the Claude/Copilot shape (Title-Case `name`, a YAML-list `tools:` using Claude tool
 * names such as `Read`/`Glob`/`Bash`). Agents without a `model:` inherit the dispatching
 * session's model. Pi's built-in tools are the
 * lowercase set `read, write, edit, bash, grep, find, ls`, so this module NORMALIZES the
 * shared shape to Pi semantics — the adapter layer absorbs the tool difference, exactly
 * like every other adapter in this corpus, so `.pi/agents/*.md` can stay pure symlinks.
 */

import * as fs from "node:fs";
import * as path from "node:path";
import { fileURLToPath } from "node:url";
import { CONFIG_DIR_NAME, getAgentDir, parseFrontmatter } from "@earendil-works/pi-coding-agent";

const corpusAgentsDir = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..", "..", "..", "..", "agents");

export type AgentScope = "user" | "project" | "both";

export interface AgentConfig {
	name: string;
	description: string;
	tools?: string[];
	/**
	 * Set when the agent declared a non-empty `tools` list of which nothing maps to a Pi tool. Such an
	 * agent must not launch: dropping the whole list would hand a deliberately restricted agent (a
	 * read-only reviewer) Pi's full default toolset. Mirrors Claude Code, which refuses to launch a
	 * subagent whose `tools` list resolves to no tool.
	 */
	toolsError?: string;
	model?: string;
	systemPrompt: string;
	source: "package" | "user" | "project";
	filePath: string;
}

export interface AgentDiscoveryResult {
	agents: AgentConfig[];
	projectAgentsDir: string | null;
}

type AgentFrontmatter = {
	name?: unknown;
	description?: unknown;
	tools?: unknown;
	model?: unknown;
};

/**
 * Map a Claude/Copilot tool name onto Pi's built-in tool name.
 *
 * Pi built-ins: read, write, edit, bash, grep, find, ls. The corpus agents use the
 * Claude vocabulary (Read/Write/Edit/Bash/Grep/Glob + orchestration tools Agent/Skill/
 * TodoWrite). `Glob` is Pi's `find`; the orchestration tools have no Pi built-in and are
 * dropped (Pi has no separate Agent/Skill/Todo tool — subagent nesting and skills are
 * reached differently). An unknown name is dropped rather than passed through, so a stray
 * value can never disable Pi's tool allowlist by naming a tool that does not exist.
 */
const TOOL_NAME_MAP: Record<string, string | null> = {
	read: "read",
	write: "write",
	edit: "edit",
	multiedit: "edit",
	bash: "bash",
	grep: "grep",
	find: "find",
	glob: "find",
	ls: "ls",
	// No Pi built-in equivalent — dropped:
	agent: null,
	task: null,
	skill: null,
	todowrite: null,
	webfetch: null,
	websearch: null,
};

/**
 * The outcome of normalizing a frontmatter `tools` value.
 *
 * `tools` is the de-duplicated Pi allowlist, or `undefined` when the agent declared no tools at
 * all (it then inherits Pi's full default toolset, exactly like an omitted `tools:` in Claude Code).
 * `unresolved` lists every declared name that is neither a Pi built-in nor a known orchestration
 * tool, so a caller can tell "nothing declared" apart from "declared, but nothing resolved".
 */
export interface NormalizedTools {
	tools?: string[];
	unresolved: string[];
}

/**
 * Normalize a frontmatter `tools` value to a de-duplicated list of Pi tool names.
 * Accepts both YAML spellings in use (`tools: [Read, Bash]` and `tools: Read, Bash`).
 */
export function normalizeToolsDetailed(value: unknown): NormalizedTools {
	const raw = Array.isArray(value) ? value : typeof value === "string" ? value.split(",") : [];
	const declared = raw
		.filter((t): t is string => typeof t === "string")
		.map((t) => t.trim())
		.filter(Boolean);
	const mapped = declared
		.map((t) => t.toLowerCase())
		.map((t) => (t in TOOL_NAME_MAP ? TOOL_NAME_MAP[t] : null))
		.filter((t): t is string => typeof t === "string");
	const unresolved = declared.filter((t) => !(t.toLowerCase() in TOOL_NAME_MAP));
	const deduped = Array.from(new Set(mapped));
	return { tools: deduped.length > 0 ? deduped : undefined, unresolved };
}

/**
 * Normalize a frontmatter `tools` value to a de-duplicated list of Pi tool names, or `undefined`
 * when nothing maps. Prefer {@link normalizeToolsDetailed} where the difference between an omitted
 * list and an unresolvable one matters — it always does when the result gates a launch.
 */
export function normalizeTools(value: unknown): string[] | undefined {
	return normalizeToolsDetailed(value).tools;
}

/**
 * The launch error for an agent whose declared tools resolve to nothing, or `undefined` when the
 * agent either declared no tools or at least one of them resolved.
 */
export function toolsErrorFor(name: string, value: unknown): string | undefined {
	const { tools, unresolved } = normalizeToolsDetailed(value);
	if (tools !== undefined || unresolved.length === 0) return undefined;
	return (
		`Agent "${name}" declares tools that resolve to no Pi tool: ${unresolved.join(", ")}. ` +
		`It would launch with every tool instead of the restriction it declares, so it is not launched. ` +
		`Declare Pi-resolvable names (Read, Write, Edit, Bash, Grep, Glob, ls) in its frontmatter.`
	);
}

function loadAgentsFromDir(dir: string, source: "package" | "user" | "project"): AgentConfig[] {
	const agents: AgentConfig[] = [];
	if (!fs.existsSync(dir)) return agents;

	let entries: fs.Dirent[];
	try {
		entries = fs.readdirSync(dir, { withFileTypes: true });
	} catch {
		return agents;
	}

	for (const entry of entries) {
		if (!entry.name.endsWith(".md")) continue;
		// Follow symlinks: the corpus adapters in .pi/agents are symlinks into .cratis/ai/agents.
		if (!entry.isFile() && !entry.isSymbolicLink()) continue;

		const filePath = path.join(dir, entry.name);
		let content: string;
		try {
			content = fs.readFileSync(filePath, "utf-8");
		} catch {
			continue;
		}

		const { frontmatter, body } = parseFrontmatter<AgentFrontmatter>(content);
		if (typeof frontmatter.name !== "string" || typeof frontmatter.description !== "string") continue;

		agents.push({
			name: frontmatter.name,
			description: frontmatter.description,
			tools: normalizeTools(frontmatter.tools),
			toolsError: toolsErrorFor(frontmatter.name, frontmatter.tools),
			model: typeof frontmatter.model === "string" ? frontmatter.model.trim() : undefined,
			systemPrompt: body,
			source,
			filePath,
		});
	}

	return agents;
}

function isDirectory(p: string): boolean {
	try {
		return fs.statSync(p).isDirectory();
	} catch {
		return false;
	}
}

/** Nearest `<ancestor>/.pi/agents` from `cwd` up to the filesystem root. */
function findNearestProjectAgentsDir(cwd: string): string | null {
	let currentDir = cwd;
	while (true) {
		const candidate = path.join(currentDir, CONFIG_DIR_NAME, "agents");
		if (isDirectory(candidate)) return candidate;
		const parentDir = path.dirname(currentDir);
		if (parentDir === currentDir) return null;
		currentDir = parentDir;
	}
}

export function discoverAgents(cwd: string, scope: AgentScope): AgentDiscoveryResult {
	const userDir = path.join(getAgentDir(), "agents");
	const projectAgentsDir = findNearestProjectAgentsDir(cwd);

	const packageAgents = scope === "project" ? [] : loadAgentsFromDir(corpusAgentsDir, "package");
	const userAgents = scope === "project" ? [] : loadAgentsFromDir(userDir, "user");
	const projectAgents = scope === "user" || !projectAgentsDir ? [] : loadAgentsFromDir(projectAgentsDir, "project");

	// User agents override package agents; project agents override both.
	const agentMap = new Map<string, AgentConfig>();
	for (const agent of packageAgents) agentMap.set(agent.name, agent);
	for (const agent of userAgents) agentMap.set(agent.name, agent);
	for (const agent of projectAgents) agentMap.set(agent.name, agent);

	return { agents: Array.from(agentMap.values()), projectAgentsDir };
}
