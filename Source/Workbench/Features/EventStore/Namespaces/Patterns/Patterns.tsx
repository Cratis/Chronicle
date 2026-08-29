// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { PivotDimension, PivotFilter, PivotGroup, PivotViewer } from '@cratis/components/PivotViewer';
import { AllPatternScopes, PatternsForScope } from 'Api/Patterns';
import { BehaviorPattern } from 'Api/Patterns/BehaviorPattern';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { useParams } from 'react-router-dom';
import { useEffect, useState } from 'react';
import { Page } from 'Components/Common/Page';
import strings from 'Strings';
import { PatternScopeSelector } from './PatternScopeSelector';
import { ConfidenceBar } from './ConfidenceBar';
import { summaryOf } from './patternSummary';

// A pattern only constrains the facets it is about, so a missing facet is not missing data — it means the pattern
// holds whatever that facet's value is. Saying "Any" keeps that readable as a pivot value.
const facet = (pattern: BehaviorPattern, name: string) => pattern.facets?.[name] ?? 'Any';

const facetAxes = [
    { key: 'commandType', name: 'CommandType', label: strings.patterns.commandType },
    { key: 'initiatorType', name: 'InitiatorType', label: strings.patterns.initiatorType },
    { key: 'day', name: 'Day', label: strings.patterns.day },
    { key: 'timeBucket', name: 'TimeBucket', label: strings.patterns.timeBucket },
    { key: 'aggregateType', name: 'AggregateType', label: strings.patterns.aggregateType },
    { key: 'causedByCommand', name: 'CausedByCommand', label: strings.patterns.causedByCommand },
];

const dimensions: PivotDimension<BehaviorPattern>[] = [
    ...facetAxes.map(({ key, name, label }) => ({
        key,
        label,
        getValue: (pattern: BehaviorPattern) => facet(pattern, name),
        // Command is the axis people land on first, so its busiest values lead; the rest read better alphabetically.
        sort: key === 'commandType'
            ? (a: PivotGroup<BehaviorPattern>, b: PivotGroup<BehaviorPattern>) => b.items.length - a.items.length
            : (a: PivotGroup<BehaviorPattern>, b: PivotGroup<BehaviorPattern>) => a.label.localeCompare(b.label),
    })),
    {
        key: 'specificity',
        label: strings.patterns.specificity,
        getValue: (pattern) => `${pattern.specificity} facet${pattern.specificity === 1 ? '' : 's'}`,
        sort: (a, b) => a.label.localeCompare(b.label),
    },
];

const filters: PivotFilter<BehaviorPattern>[] = [
    ...facetAxes.map(({ key, name, label }) => ({
        key,
        label,
        getValue: (pattern: BehaviorPattern) => facet(pattern, name),
        multi: true,
    })),
    { key: 'confidence', label: strings.patterns.confidence, getValue: (pattern) => Math.round(pattern.confidence * 100), type: 'number', buckets: 10 },
    { key: 'occurrences', label: strings.patterns.occurrences, getValue: (pattern) => Number(pattern.occurrences), type: 'number', buckets: 10 },
];

const detailRenderer = (pattern: BehaviorPattern) => (
    <div className="p-5 h-full overflow-auto">
        <h2 className="mt-0 mb-5">{summaryOf(pattern)}</h2>

        <div className="mb-6 max-w-md">
            <ConfidenceBar value={pattern.confidence} label={strings.patterns.confidence} />
            <div className="mt-2">
                <ConfidenceBar value={pattern.support} label={strings.patterns.support} />
            </div>
        </div>

        <h3>{strings.patterns.facets}</h3>
        <table className="w-full max-w-md mb-6">
            <tbody>
                {Object.entries(pattern.facets ?? {}).map(([name, value]) => (
                    <tr key={name}>
                        <td className="py-1 pr-4 opacity-70">{name}</td>
                        <td className="py-1 font-medium">{value}</td>
                    </tr>
                ))}
            </tbody>
        </table>

        <table className="w-full max-w-md">
            <tbody>
                <tr><td className="py-1 pr-4 opacity-70">{strings.patterns.occurrences}</td><td className="py-1">{String(pattern.occurrences)}</td></tr>
                <tr><td className="py-1 pr-4 opacity-70">{strings.patterns.weight}</td><td className="py-1">{pattern.weight.toFixed(2)}</td></tr>
                <tr><td className="py-1 pr-4 opacity-70">{strings.patterns.firstSeen}</td><td className="py-1">{pattern.firstSeen.toLocaleString()}</td></tr>
                <tr><td className="py-1 pr-4 opacity-70">{strings.patterns.lastSeen}</td><td className="py-1">{pattern.lastSeen.toLocaleString()}</td></tr>
            </tbody>
        </table>
    </div>
);

export const Patterns = () => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const [scope, setScope] = useState<string | undefined>(undefined);

    const [scopes] = AllPatternScopes.use({ eventStore: params.eventStore!, namespace: params.namespace! });
    const scopeIds = (scopes.data ?? []).map((_) => _.id);

    useEffect(() => {
        if (!scope && scopeIds.length > 0) {
            setScope(scopeIds[0]);
        }
    }, [scope, scopeIds]);

    const [patterns] = PatternsForScope
        .when(!!scope)
        .use({ eventStore: params.eventStore!, namespace: params.namespace!, groupingKey: scope ?? '' });

    return (
        <Page title={strings.mainMenu.patterns} noBackground noPadding>
            <div className="p-4 h-full flex flex-col min-h-0 gap-4">
                <PatternScopeSelector scopes={scopeIds} selected={scope} onChange={setScope} />

                <PivotViewer<BehaviorPattern>
                    data={patterns.data ?? []}
                    dimensions={dimensions}
                    filters={filters}
                    defaultDimensionKey="commandType"
                    cardRenderer={(pattern) => ({
                        title: summaryOf(pattern),
                        labels: [strings.patterns.confidence, strings.patterns.occurrences],
                        values: [`${Math.round(pattern.confidence * 100)}%`, String(pattern.occurrences)],
                    })}
                    detailRenderer={detailRenderer}
                    getItemId={(pattern) => pattern.id}
                    searchFields={[
                        (pattern) => Object.values(pattern.facets ?? {}).join(' '),
                        (pattern) => pattern.groupingKey
                    ]}
                    className="flex-1 min-h-0"
                    emptyContent={<span>{scopeIds.length === 0 ? strings.patterns.noScopes : strings.patterns.noPatterns}</span>}
                    isLoading={patterns.isPerforming || scopes.isPerforming}
                />
            </div>
        </Page>
    );
};
