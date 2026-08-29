// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AllPatternScopes, PatternsForScope } from 'Api/Patterns';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { useParams } from 'react-router-dom';
import { useEffect, useMemo, useState } from 'react';
import { Page } from 'Components/Common/Page';
import strings from 'Strings';
import { PatternScopeSelector } from './PatternScopeSelector';
import { ConfidenceBar } from './ConfidenceBar';
import {
    Slot,
    days,
    patternsInSlot as patternsFor,
    slotFor,
    slotKey,
    strongestBySlot as strongestPerSlot,
    timeBucketLabels,
    timeBuckets
} from './PatternHeatmapState';

const cellColor = (confidence: number | undefined) => {
    if (confidence === undefined) {
        return 'var(--surface-100)';
    }

    // One hue at varying strength rather than a rainbow ramp: the quantity is one-dimensional, and lightness of a
    // single hue stays readable for anyone who cannot separate hues.
    return `color-mix(in srgb, var(--primary-color) ${Math.round(confidence * 100)}%, var(--surface-100))`;
};


export const PatternHeatmap = () => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const [scope, setScope] = useState<string | undefined>(undefined);
    const [selectedSlot, setSelectedSlot] = useState<Slot | undefined>(undefined);

    const [scopes] = AllPatternScopes.use({ eventStore: params.eventStore!, namespace: params.namespace! });
    const scopeIds = useMemo(() => (scopes.data ?? []).map((_) => _.id), [scopes.data]);

    useEffect(() => {
        if (!scope && scopeIds.length > 0) {
            setScope(scopeIds[0]);
        }
    }, [scope, scopeIds]);

    const [patterns] = PatternsForScope
        .when(!!scope)
        .use({ eventStore: params.eventStore!, namespace: params.namespace!, groupingKey: scope ?? '' });

    const all = useMemo(() => patterns.data ?? [], [patterns.data]);

    const strongestBySlot = useMemo(() => strongestPerSlot(all), [all]);

    const nowSlot = slotFor(new Date());
    const nowPattern = strongestBySlot.get(slotKey(nowSlot));
    const inSelectedSlot = selectedSlot ? patternsFor(all, selectedSlot) : [];

    return (
        <Page title={strings.mainMenu.patternHeatmap} noBackground>
            <div className="flex flex-col gap-4 h-full min-h-0">
                <PatternScopeSelector scopes={scopeIds} selected={scope} onChange={setScope} />

                {nowPattern && (
                    <div className="p-4 rounded border border-[var(--surface-border)]">
                        <div className="text-sm opacity-70 mb-1">
                            {nowSlot.day}, {timeBucketLabels[nowSlot.timeBucket]}
                        </div>
                        <div className="text-lg">
                            Right now, <strong>{scope}</strong> usually{' '}
                            <strong>{nowPattern.facets?.['CommandType'] ?? 'does this'}</strong>
                        </div>
                        <div className="mt-2 max-w-sm">
                            <ConfidenceBar value={nowPattern.confidence} label={strings.patterns.confidence} />
                        </div>
                    </div>
                )}

                <div className="flex gap-6 flex-1 min-h-0">
                    <div className="overflow-auto">
                        <table className="border-separate border-spacing-1">
                            <thead>
                                <tr>
                                    <th />
                                    {timeBuckets.map((bucket) => (
                                        <th key={bucket} className="text-xs font-normal opacity-70 px-2 whitespace-nowrap">
                                            {timeBucketLabels[bucket]}
                                        </th>
                                    ))}
                                </tr>
                            </thead>
                            <tbody>
                                {days.map((day) => (
                                    <tr key={day}>
                                        <th className="text-xs font-normal opacity-70 pr-2 text-right whitespace-nowrap">{day}</th>
                                        {timeBuckets.map((bucket) => {
                                            const strongest = strongestBySlot.get(slotKey({ day, timeBucket: bucket }));
                                            const isSelected = selectedSlot?.day === day && selectedSlot?.timeBucket === bucket;
                                            const isNow = nowSlot.day === day && nowSlot.timeBucket === bucket;

                                            return (
                                                <td key={bucket} className="p-0">
                                                    <button
                                                        type="button"
                                                        aria-label={`${day} ${timeBucketLabels[bucket]}${strongest ? `, ${Math.round(strongest.confidence * 100)}% confident` : ', nothing established'}`}
                                                        aria-pressed={isSelected}
                                                        onClick={() => setSelectedSlot({ day, timeBucket: bucket })}
                                                        className="w-16 h-12 rounded cursor-pointer border"
                                                        style={{
                                                            backgroundColor: cellColor(strongest?.confidence),
                                                            borderColor: isSelected
                                                                ? 'var(--primary-color)'
                                                                : isNow ? 'var(--text-color)' : 'transparent',
                                                            borderWidth: isSelected || isNow ? '2px' : '1px'
                                                        }}>
                                                        <span className="text-xs tabular-nums">
                                                            {strongest ? `${Math.round(strongest.confidence * 100)}%` : ''}
                                                        </span>
                                                    </button>
                                                </td>
                                            );
                                        })}
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>

                    <div className="flex-1 min-w-0 overflow-auto">
                        {!selectedSlot && <p className="opacity-70">{strings.patterns.selectSlot}</p>}
                        {selectedSlot && inSelectedSlot.length === 0 && <p className="opacity-70">{strings.patterns.noPatternInSlot}</p>}
                        {selectedSlot && inSelectedSlot.length > 0 && (
                            <>
                                <h3 className="mt-0">
                                    {selectedSlot.day}, {timeBucketLabels[selectedSlot.timeBucket]}
                                </h3>
                                {inSelectedSlot.map((pattern) => (
                                        <div key={pattern.id} className="mb-4 p-3 rounded border border-[var(--surface-border)]">
                                            <div className="font-medium mb-2">{pattern.facets?.['CommandType'] ?? '—'}</div>
                                            <ConfidenceBar value={pattern.confidence} label={strings.patterns.confidence} />
                                            <div className="mt-1">
                                                <ConfidenceBar value={pattern.support} label={strings.patterns.support} />
                                            </div>
                                            <div className="text-xs opacity-70 mt-2">
                                                {strings.patterns.occurrences}: {String(pattern.occurrences)} · {strings.patterns.lastSeen}: {pattern.lastSeen.toLocaleDateString()}
                                            </div>
                                        </div>
                                    ))}
                            </>
                        )}
                    </div>
                </div>
            </div>
        </Page>
    );
};
