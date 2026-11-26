// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createElement } from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import sinon from 'sinon';
import { ComparisonPanel } from '../ComparisonPanel';
import { ComparisonViewModel } from '../ComparisonViewModel';
import { eventWith, revision, scope } from '../given/events';

const render = (viewModel: ComparisonViewModel) => renderToStaticMarkup(createElement(ComparisonPanel, { viewModel }));

const modelFor = (event: ReturnType<typeof eventWith>, open = true) => {
    const viewModel = new ComparisonViewModel();
    viewModel.handleProps({ scope, event });
    if (open) viewModel.open();
    return viewModel;
};

describe('when the comparison is closed', () => {
    const html = render(modelFor(eventWith([revision('{}', 2, 5)]), false));
    it('should offer an explicit open control', () => html.should.include('Compare revisions'));
    it('should show no rows', () => html.should.not.include('<table'));
});

describe('when comparing differing revisions', () => {
    const html = render(modelFor(eventWith([revision('{"quantity":3}', 3, 5)])));
    it('should associate both labels with their selects', () => {
        for (const id of [...html.matchAll(/<label for="([^"]+)"/g)].map(match => match[1])) html.should.include(`<select id="${id}"`);
        [...html.matchAll(/<label for=/g)].length.should.equal(2);
    });
    it('should use unique control ids', () => {
        const ids = [...html.matchAll(/<select id="([^"]+)"/g)].map(match => match[1]);
        new Set(ids).size.should.equal(2);
    });
    it('should show visible generations', () => html.should.include('Generation 3').and.include('Generation 1'));
    it('should have a caption and column headers', () => html.should.include('<caption>').and.include('scope="col"'));
    it('should show the raw path and change kind as text', () => html.should.include('/quantity').and.include('Changed'));
    it('should show a missing value distinctly from null', () => html.should.include('(absent)').and.include('/status'));
    it('should announce status', () => html.should.include('role="status"'));
    it('should make the scroll region keyboard reachable', () => html.should.include('tabindex="0"'));
});

describe('when payload contains html-like text', () => {
    const html = render(modelFor(eventWith([revision('{"quantity":"<img src=x onerror=alert(1)>"}', 2, 5)])));
    it('should not emit an element', () => html.should.not.include('<img'));
    it('should escape it', () => html.should.include('&lt;img'));
});

for (const [name, event, message] of [
    ['redacted', eventWith([revision('{"secret":"leak"}', 2, 5)], 'EventRedacted'), 'redacted'],
    ['without history', eventWith([]), 'nothing to compare']
] as const) {
    describe(`when the event is ${name}`, () => {
        const html = render(modelFor(event));
        it('should say so', () => html.should.include(message));
        it('should show no rows', () => html.should.not.include('<table'));
        it('should show no payload', () => html.should.not.include('leak'));
    });
}

describe('when a side is invalid', () => {
    const html = render(modelFor(eventWith([revision('{oops', 2, 5)])));
    it('should say it is not valid JSON', () => html.should.include('not valid JSON'));
    it('should show no rows', () => html.should.not.include('<table'));
});

describe('when a side is limited', () => {
    const html = render(modelFor(eventWith([revision(`{"v":${'['.repeat(40)}${']'.repeat(40)}}`, 2, 5)])));
    it('should say that no comparison was made', () => html.should.include('nested too deeply'));
});

describe('when opening, selecting and closing', () => {
    const fetch = sinon.stub(globalThis, 'fetch');
    const viewModel = modelFor(eventWith([revision('{}', 2, 5)]), false);
    viewModel.open();
    viewModel.selectLeft(viewModel.revisions.choices[1].key);
    viewModel.close();
    render(viewModel);
    afterAll(() => fetch.restore());
    it('should make no network request', () => fetch.called.should.be.false);
    it('should take no collaborators', () => ComparisonViewModel.length.should.equal(0));
});
