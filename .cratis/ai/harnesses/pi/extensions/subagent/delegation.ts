// cratis-ai-managed: harnesses/pi/extensions/subagent/delegation.ts
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Deciding whether the Cratis `subagent` tool stands down for a session.
 *
 * Widely used delegation extensions already read `.pi/agents/*.md` — pi-subagents registers a tool named
 * `Agent` that lists every Cratis corpus agent. Offering that tool and `subagent` side by side makes the
 * model guess which to call and pay for both descriptions, so the Cratis tool yields to the one the user
 * installed deliberately.
 *
 * The match is on the exact tool name, not a substring: `agent` is a common word in unrelated tool names,
 * and silently withdrawing delegation against one of those would be a bug nobody could see.
 *
 * Kept free of host calls so the policy is testable from the tool list alone; the extension owns
 * `getAllTools`, `setActiveTools` and the notice.
 */

import type { ToolInfo } from "@earendil-works/pi-coding-agent";

/** The name the Cratis delegation tool registers under. */
export const SUBAGENT_TOOL_NAME = "subagent";

/** Tool names that mean "another extension already delegates to agents". */
export const FOREIGN_DELEGATION_TOOL_NAMES: ReadonlySet<string> = new Set(["Agent"]);

/** The registered delegation tool the Cratis tool yields to, or `undefined` when there is none. */
export function foreignDelegationTool<T extends Pick<ToolInfo, "name">>(tools: readonly T[]): T | undefined {
	return tools.find((tool) => FOREIGN_DELEGATION_TOOL_NAMES.has(tool.name));
}

/** The one informational notice shown when the Cratis tool stands down. */
export function standDownNotice(tool: Pick<ToolInfo, "name"> & { sourceInfo?: Partial<ToolInfo["sourceInfo"]> }): string {
	const source = tool.sourceInfo?.source ?? "another extension";
	return (
		`The "${tool.name}" tool from ${source} already delegates to agents, so the Cratis ` +
		`"${SUBAGENT_TOOL_NAME}" tool is disabled for this session to avoid offering two delegation tools. ` +
		`Remove that extension to use "${SUBAGENT_TOOL_NAME}" instead.`
	);
}
