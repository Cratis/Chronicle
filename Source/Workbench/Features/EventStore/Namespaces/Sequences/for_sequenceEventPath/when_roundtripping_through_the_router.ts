// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createMemoryRouter } from 'react-router-dom';
import { sequenceEventPath, sequenceIdFromSearch } from '../sequenceEventPath';
import { SequenceEventDetailsViewModel } from '../SequenceEventDetailsViewModel';
import { AppendedEvents } from 'Features/Sequences';
import sinon from 'sinon';

const identities = ['a%2Fb', 'a%2fb', 'a/b', '.', '..', 'a/../b', '事件/å', 'a+b', '123', 'true'];

for (const basename of ['', '/workbench']) {
    for (const identity of identities) {
        describe(`when routing sequence ${identity} under ${basename || 'root'}`, () => {
            let query: sinon.SinonStubbedInstance<AppendedEvents>;
            let routeNumber: string | undefined;
            let pathname: string;

            beforeEach(async () => {
                const href = sequenceEventPath('store', 'namespace', identity, 0, 'failure')!;
                const url = new URL(`${basename}${href}`, 'https://unit.invalid');
                const router = createMemoryRouter([
                    { path: '/event-store/:eventStore/:namespace/sequences/event/:sequenceNumber', element: null }
                ], { basename: basename || '/', initialEntries: [url.pathname + url.search] });
                const match = router.state.matches[0];
                routeNumber = match.params.sequenceNumber;
                pathname = router.state.location.pathname;
                query = sinon.createStubInstance(AppendedEvents);
                query.perform.resolves({ isSuccess: true, data: [] } as never);
                const vm = new SequenceEventDetailsViewModel(query);
                await vm.handleProps({ eventStore: match.params.eventStore!, namespace: match.params.namespace!,
                    sequenceNumber: routeNumber, eventSequenceId: sequenceIdFromSearch(new URLSearchParams(router.state.location.search)) });
                router.dispose();
            });

            it('should keep the exact opaque identity at the query boundary', () => query.perform.firstCall.args[0]!.eventSequenceId.should.equal(identity));
            it('should keep the numeric path parameter', () => routeNumber!.should.equal('0'));
            it('should keep identity out of the pathname', () => pathname.should.equal(`${basename}/event-store/store/namespace/sequences/event/0`));
        });
    }
}

describe('when sequence identity is not representable or ambiguous', () => {
    it('should reject a missing key', () => (sequenceIdFromSearch(new URLSearchParams()) === undefined).should.be.true);
    it('should reject duplicate keys', () => (sequenceIdFromSearch(new URLSearchParams('eventSequenceId=a&eventSequenceId=a')) === undefined).should.be.true);
    it('should not query an ambiguous identity', async () => {
        const query = sinon.createStubInstance(AppendedEvents);
        const vm = new SequenceEventDetailsViewModel(query);
        await vm.handleProps({ eventStore: 'store', namespace: 'namespace', sequenceNumber: '0',
            eventSequenceId: sequenceIdFromSearch(new URLSearchParams('eventSequenceId=a&eventSequenceId=b')) });
        query.perform.called.should.be.false;
    });
    it('should reject an empty identity', () => (sequenceEventPath('s', 'n', '', 0) === undefined).should.be.true);
    it('should reject a lone high surrogate', () => (sequenceEventPath('s', 'n', '\ud800', 0) === undefined).should.be.true);
    it('should reject a lone low surrogate', () => (sequenceEventPath('s', 'n', '\udc00', 0) === undefined).should.be.true);
});
