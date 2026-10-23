// cratis-ai-managed: harnesses/pi/extensions/subagent/tool-names.ts
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

/** Generation accepts only explicitly mapped names, never inherited JavaScript object properties. */
export function isKnownToolName(name: string): boolean {
	return Object.hasOwn(TOOL_NAME_MAP, name.trim().toLowerCase());
}

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
