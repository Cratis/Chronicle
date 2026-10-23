// cratis-ai-managed: harnesses/pi/extensions/subagent/agents.ts
/**
 * Agent discovery + format normalization for the Cratis subagent tool.
 *
 * The agent definitions are the SINGLE-SOURCE corpus files under `.cratis/ai/agents/*.md`,
 * surfaced to Pi through generated Pi-native adapters in `.pi/agents/*.md`. The canonical
 * files use Claude/Copilot tool names such as `Read`/`Glob`/`Bash`; generated files use
 * Pi's lowercase names. This loader also normalizes canonical, user and legacy files
 * with the same tool-name map used by generation. Agents without a `model:` inherit
 * the dispatching session's model.
 */

import * as fs from "node:fs";
import * as path from "node:path";
import { fileURLToPath } from "node:url";
import { CONFIG_DIR_NAME, getAgentDir, parseFrontmatter } from "@earendil-works/pi-coding-agent";
import { normalizeTools, normalizeToolsDetailed } from "./tool-names.ts";
export { normalizeTools, normalizeToolsDetailed, type NormalizedTools } from "./tool-names.ts";

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
		// Follow symlinks too, for user agents and legacy canonical adapters.
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
