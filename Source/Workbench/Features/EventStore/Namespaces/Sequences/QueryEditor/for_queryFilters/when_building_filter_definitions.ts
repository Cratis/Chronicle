// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { buildFilterDefinitions, type QueryFilterLabels } from '../queryFilters';

const labels: QueryFilterLabels = {
    eventType: 'Hendelsestyper',
    eventSource: 'Hendelseskilde',
    eventSourceType: 'Hendelseskildetype',
    eventStreamType: 'Hendelsesstrømtype',
    correlation: 'Korrelasjon',
    tags: 'Etiketter',
    occurred: 'Tidspunkt',
    searchEventTypes: 'Søk etter hendelsestyper'
};

describe('when building filter definitions without event types', () => {
    const definition = buildFilterDefinitions([], labels)[0];

    it('should leave search visibility to the shared filter', () =>
        (definition.searchable === undefined).should.be.true);
    it('should keep the options empty', () => definition.options!.should.eql([]));
});

describe('when building filter definitions with a short event type list', () => {
    const definition = buildFilterDefinitions(['Registered', 'Archived'], labels)[0];

    it('should leave search visibility to the shared filter', () =>
        (definition.searchable === undefined).should.be.true);
    it('should preserve each event type as its option key, label and value', () =>
        definition.options!.should.eql([
            { key: 'Registered', label: 'Registered', value: 'Registered' },
            { key: 'Archived', label: 'Archived', value: 'Archived' }
        ]));
});

describe('when building filter definitions with a long event type list', () => {
    const eventTypeIds = Array.from({ length: 30 }, (_, index) => `EventType${index}`);
    const definition = buildFilterDefinitions(eventTypeIds, labels)[0];

    it('should leave search visibility to the shared filter', () =>
        (definition.searchable === undefined).should.be.true);
    it('should preserve all event type options in order', () =>
        definition.options!.should.eql(eventTypeIds.map(id => ({ key: id, label: id, value: id }))));
});

describe('when building localized filter definitions', () => {
    const definitions = buildFilterDefinitions(['Registered'], labels);
    const eventType = definitions[0];

    it('should preserve the event type key', () => eventType.key.should.equal('eventType'));
    it('should use the localized event type label', () => eventType.label.should.equal(labels.eventType));
    it('should keep the event type facet a string filter', () => eventType.type.should.equal('string'));
    it('should allow multiple event types', () => (eventType.multi === true).should.be.true);
    it('should use the localized search placeholder', () =>
        eventType.searchPlaceholder!.should.equal(labels.searchEventTypes));
    it('should preserve the custom groups and their localized labels', () =>
        definitions.slice(1).should.eql([
            { key: 'eventSource', label: labels.eventSource, type: 'custom' },
            { key: 'eventSourceType', label: labels.eventSourceType, type: 'custom' },
            { key: 'eventStreamType', label: labels.eventStreamType, type: 'custom' },
            { key: 'correlation', label: labels.correlation, type: 'custom' },
            { key: 'tags', label: labels.tags, type: 'custom' },
            { key: 'occurred', label: labels.occurred, type: 'custom' }
        ]));
});
