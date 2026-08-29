// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export interface ConfidenceBarProps {
    value: number;
    label?: string;
}

/**
 * Renders a confidence as a bar plus its number.
 *
 * The bar is what makes a strong pattern distinguishable from a weak one at a glance; the number is what makes it
 * answerable. A bar on its own invites reading a length as a fact, so both are shown.
 */
export const ConfidenceBar = ({ value, label }: ConfidenceBarProps) => {
    const percentage = Math.round(Math.min(Math.max(value, 0), 1) * 100);

    return (
        <div className="flex items-center gap-2 w-full" title={`${percentage}%`}>
            {label && <span className="text-xs opacity-70 shrink-0">{label}</span>}
            <div className="flex-1 h-2 rounded bg-[var(--surface-300)] overflow-hidden">
                <div
                    className="h-full rounded"
                    style={{
                        width: `${percentage}%`,
                        backgroundColor: percentage >= 80
                            ? 'var(--green-500)'
                            : percentage >= 50 ? 'var(--yellow-500)' : 'var(--orange-500)'
                    }}
                />
            </div>
            <span className="text-xs tabular-nums shrink-0">{percentage}%</span>
        </div>
    );
};
