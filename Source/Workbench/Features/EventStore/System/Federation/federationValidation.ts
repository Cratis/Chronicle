// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

const pinPattern = /^sha256:[A-Za-z0-9+/]{43}=$/;

/**
 * Checks that a certificate pin has the form `sha256:` followed by a 44-character base64 digest.
 * @param pin The pin to check.
 * @returns True when the pin is well formed.
 */
export const isValidPin = (pin: string | undefined): boolean => pinPattern.test((pin ?? '').trim());

/**
 * Checks that an endpoint is an absolute https URL.
 * @param endpoint The endpoint to check.
 * @returns True when the endpoint is a valid https URL.
 */
export const isValidHttpsEndpoint = (endpoint: string | undefined): boolean => {
    try {
        return new URL((endpoint ?? '').trim()).protocol === 'https:';
    } catch {
        return false;
    }
};

/**
 * Checks that a value holds something other than whitespace.
 * @param value The value to check.
 * @returns True when the value is not blank.
 */
export const isNotBlank = (value: string | undefined): boolean => (value ?? '').trim().length > 0;

/**
 * Splits a comma or newline separated list into trimmed, distinct, non-empty entries.
 * @param text The text to split.
 * @returns The entries.
 */
export const parseList = (text: string): string[] =>
    Array.from(new Set(text.split(/[,\n]/).map(entry => entry.trim()).filter(entry => entry.length > 0)));

/**
 * Formats a list for editing in a single text input.
 * @param values The entries.
 * @returns The entries joined with a comma and a space.
 */
export const formatList = (values: string[] | undefined): string => (values ?? []).join(', ');

/**
 * Formats a byte count using binary units.
 * @param bytes The number of bytes.
 * @returns A human readable size.
 */
export const formatBytes = (bytes: number): string => {
    if (!bytes || bytes < 1024) {
        return `${bytes ?? 0} B`;
    }
    const units = ['KiB', 'MiB', 'GiB', 'TiB'];
    let value = bytes / 1024;
    let unitIndex = 0;
    while (value >= 1024 && unitIndex < units.length - 1) {
        value /= 1024;
        unitIndex++;
    }
    return `${value.toFixed(1)} ${units[unitIndex]}`;
};
