/**
 * Agent discovery + format normalization for the Cratis subagent tool.
 *
 * The agent definitions are the SINGLE-SOURCE corpus files under `.ai/agents/*.md`,
 * surfaced to Pi through symlink adapters in `.pi/agents/*.md`. Those files are written
 * in the Claude/Copilot shape (Title-Case `name`, a YAML-list `tools:` using Claude tool
 * names such as `Read`/`Glob`/`Bash`, and a `model:` id). Pi's built-in tools are the
 * lowercase set `read, write, edit, bash, grep, find, ls`, so this module NORMALIZES the
 * shared shape to Pi semantics — the adapter layer absorbs the tool difference, exactly
 * like every other adapter in this corpus, so `.pi/agents/*.md` can stay pure symlinks.
 */

import * as fs from "node:fs";
import * as path from "node:path";
import { CONFIG_DIR_NAME, getAgentDir, parseFrontmatter } from "@earendil-works/pi-coding-agent";

export type AgentScope = "user" | "project" | "both";

export interface AgentConfig {
	name: string;
	description: string;
	tools?: string[];
	model?: string;
	systemPrompt: string;
	source: "user" | "project";
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
 * Normalize a frontmatter `tools` value to a de-duplicated list of Pi tool names.
 * Accepts both YAML spellings in use (`tools: [Read, Bash]` and `tools: Read, Bash`).
 * Returns `undefined` when nothing maps, so the subagent inherits Pi's full default
 * toolset rather than being launched with an empty allowlist.
 */
export function normalizeTools(value: unknown): string[] | undefined {
	const raw = Array.isArray(value) ? value : typeof value === "string" ? value.split(",") : [];
	const mapped = raw
		.filter((t): t is string => typeof t === "string")
		.map((t) => t.trim().toLowerCase())
		.filter(Boolean)
		.map((t) => (t in TOOL_NAME_MAP ? TOOL_NAME_MAP[t] : null))
		.filter((t): t is string => typeof t === "string");
	const deduped = Array.from(new Set(mapped));
	return deduped.length > 0 ? deduped : undefined;
}

function loadAgentsFromDir(dir: string, source: "user" | "project"): AgentConfig[] {
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
		// Follow symlinks: the corpus adapters in .pi/agents are symlinks into .ai/agents.
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

	const userAgents = scope === "project" ? [] : loadAgentsFromDir(userDir, "user");
	const projectAgents = scope === "user" || !projectAgentsDir ? [] : loadAgentsFromDir(projectAgentsDir, "project");

	// Project agents override user agents of the same name.
	const agentMap = new Map<string, AgentConfig>();
	for (const agent of userAgents) agentMap.set(agent.name, agent);
	for (const agent of projectAgents) agentMap.set(agent.name, agent);

	return { agents: Array.from(agentMap.values()), projectAgentsDir };
}
