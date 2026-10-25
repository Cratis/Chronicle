// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createElement, type ReactNode } from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import { vi } from 'vitest';
import { ClosedStream } from 'Features/Sequences/ClosedStream';
import { ClosedStreamOrigin } from 'Features/Sequences/ClosedStreamOrigin';
import { ClosedStreams } from '../ClosedStreams';

const state = vi.hoisted(() => ({ item: undefined as ClosedStream | undefined }));

vi.mock('react-router-dom', () => ({ useParams: () => ({ eventStore: 'store', namespace: 'namespace' }) }));
vi.mock('Components/Common/Page', () => ({ Page: ({ children }: { children?: ReactNode }) => children }));
vi.mock('@cratis/components/DataPage', () => {
    const content = ({ children }: { children?: ReactNode }) => children;
    return {
        DataPage: Object.assign(content, { Columns: content }),
        Column: ({ field, body }: { field: keyof ClosedStream; body?: (item: ClosedStream) => ReactNode }) =>
            createElement('div', {}, body ? body(state.item!) : String(state.item![field] ?? '')),
    };
});

describe('when rendering a stream-scoped manual closure', () => {
    let markup: string;

    beforeEach(() => {
        state.item = Object.assign(new ClosedStream(), { origin: ClosedStreamOrigin.completeStream, sequenceNumber: 1 });
        markup = renderToStaticMarkup(createElement(ClosedStreams));
    });

    it('should identify all event sources', () => markup.should.include('Any event source'));
    it('should identify all event source types', () => markup.should.include('Any event source type'));
    it('should explain the scope of unspecified dimensions', () => markup.should.include('title="Unspecified dimensions cover all values."'));
    it('should identify a manual closure without a constraint', () => markup.should.include('—, closed manually'));
});

describe('when rendering a closure with specified dimensions', () => {
    let markup: string;

    beforeEach(() => {
        state.item = Object.assign(new ClosedStream(), {
            eventSourceId: 'source-1', eventSourceType: 'Customer', closedBy: 'Approval',
            origin: ClosedStreamOrigin.closingEvent, sequenceNumber: 2,
        });
        markup = renderToStaticMarkup(createElement(ClosedStreams));
    });

    it('should preserve the event source identifier', () => markup.should.include('source-1'));
    it('should preserve the event source type', () => markup.should.include('Customer'));
    it('should preserve the closing constraint', () => markup.should.include('Approval'));
    it('should not describe a manual closure', () => markup.should.not.include('closed manually'));
});
