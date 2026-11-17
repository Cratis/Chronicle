// cratis-ai-managed: harnesses/pi/extensions/subagent/modelTiers.ts
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as fs from 'node:fs';
import { join } from 'node:path';

export interface ModelTierConfig {
    schemaVersion: string;
    tiers: Partial<Record<string, string | string[]>>;
}

export interface ModelTierInputs {
    agent: { model?: string; modelTier?: string; reviewIndependence?: string };
    dispatchModel?: string;
    config?: ModelTierConfig;
    available?: string[];
    notice?: string;
}

export interface ModelResolution {
    model?: string;
    thinkingInherited: boolean;
    outcome: 'explicit' | 'tier' | 'inherited' | 'single-provider';
    notice?: string;
}

const maximumConfigBytes = 65536;
const knownTiers = ['scoped', 'strongest', 'reviewer'];

function record(value: unknown): Record<string, unknown> {
    if (Object.prototype.toString.call(value) !== '[object Object]') throw new Error('Expected an object');
    return value as Record<string, unknown>;
}

export function providerOf(model?: string): string | undefined {
    const separator = model?.indexOf('/') ?? -1;
    return separator > 0 ? model!.slice(0, separator) : undefined;
}

function configuredModel(value: unknown): string {
    if (typeof value !== 'string') throw new Error('Expected a model string');
    const model = value.trim();
    if (!/^[^/\s]+\/[^\s]*[^/\s]$/.test(model)) throw new Error('Expected provider/id');
    return model;
}

function parseConfig(value: unknown): ModelTierConfig {
    const parsed = record(value);
    if (typeof parsed.schemaVersion !== 'string' || !/^1\.\d+$/.test(parsed.schemaVersion)) throw new Error('Unsupported schema');
    const tiers = record(parsed.tiers);
    const config: ModelTierConfig = { schemaVersion: parsed.schemaVersion, tiers: {} };
    for (const tier of knownTiers) {
        if (!(tier in tiers)) continue;
        const candidates = tiers[tier];
        config.tiers[tier] = Array.isArray(candidates) ? candidates.map(configuredModel) : configuredModel(candidates);
    }
    return config;
}

/** Read only user-owned configuration; malformed input must never prevent dispatch. */
export function readModelTierConfig(directory: string): { config?: ModelTierConfig; notice?: string } {
    let descriptor: number | undefined;
    try {
        descriptor = fs.openSync(join(directory, 'cratis-models.json'), 'r');
        const buffer = Buffer.alloc(maximumConfigBytes + 1);
        const length = fs.readSync(descriptor, buffer, 0, buffer.length, 0);
        if (length > maximumConfigBytes) throw new Error('Configuration too large');
        return { config: parseConfig(JSON.parse(buffer.toString('utf8', 0, length))) };
    } catch (error) {
        if ((error as NodeJS.ErrnoException).code === 'ENOENT') return {};
        return { notice: 'Ignoring invalid cratis-models.json (expected schema 1.x tier mappings of provider/id; maximum 64 KiB).' };
    } finally {
        if (descriptor !== undefined) fs.closeSync(descriptor);
    }
}

function candidatesFor({ agent, config, available }: ModelTierInputs): string[] {
    const configured = agent.modelTier ? config?.tiers[agent.modelTier] : undefined;
    const candidates = typeof configured === 'string' ? [configured] : configured ?? [];
    return candidates.filter(model => available === undefined || available.includes(model));
}

function singleProvider(model: string | undefined): ModelResolution {
    return {
        model,
        thinkingInherited: false,
        outcome: 'single-provider',
        notice: `single-provider: no different-provider model configured/available for reviewer tier; reviewing on ${model ?? '(session default)'} — not independent`,
    };
}

function differentProvider(model: string, dispatchModel?: string): boolean {
    const authorProvider = providerOf(dispatchModel);
    const candidateProvider = providerOf(model);
    if (!authorProvider || !candidateProvider) return false;
    return candidateProvider !== authorProvider;
}

function explicitModel(inputs: ModelTierInputs, model: string): ModelResolution {
    if (inputs.agent.reviewIndependence === 'different-provider' && !differentProvider(model, inputs.dispatchModel)) return singleProvider(model);
    return { model, thinkingInherited: false, outcome: 'explicit' };
}

function independentModel(inputs: ModelTierInputs, candidates: string[]): ModelResolution {
    const model = candidates.find(candidate => differentProvider(candidate, inputs.dispatchModel));
    return model ? { model, thinkingInherited: false, outcome: 'tier' } : singleProvider(candidates[0] ?? inputs.dispatchModel);
}

function chooseModel(inputs: ModelTierInputs): ModelResolution {
    if (inputs.agent.model) return explicitModel(inputs, inputs.agent.model);
    const candidates = candidatesFor(inputs);
    if (inputs.agent.reviewIndependence === 'different-provider') return independentModel(inputs, candidates);
    return candidates.length > 0
        ? { model: candidates[0], thinkingInherited: false, outcome: 'tier' }
        : { model: inputs.dispatchModel, thinkingInherited: true, outcome: 'inherited' };
}

/** Resolve explicit overrides, then user tiers, then the dispatching session, without I/O. */
export function resolveModel(inputs: ModelTierInputs): ModelResolution {
    const resolution = chooseModel(inputs);
    if (inputs.notice) resolution.notice = [inputs.notice, resolution.notice].filter(Boolean).join('\n');
    return resolution;
}
