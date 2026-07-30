// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Resolves the key of a read model instance.
 *
 * Instances are the raw documents from the read model store, so the key normally sits in `_id`.
 * Instances reconstructed from a URL segment carry a plain `id` instead, so both are honored.
 * @param instance The instance to resolve the key for.
 * @returns The key, or an empty string when the instance carries none.
 */
export const getInstanceKey = (instance: unknown): string => {
    if (!instance || typeof instance !== 'object') return '';
    const candidate = (instance as Record<string, unknown>)._id ?? (instance as Record<string, unknown>).id;
    return candidate === undefined || candidate === null ? '' : String(candidate);
};
