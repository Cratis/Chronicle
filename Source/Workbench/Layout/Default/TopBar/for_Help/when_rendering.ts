// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createElement, type ReactNode } from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import { vi } from 'vitest';
import strings from 'Strings';
import { Help } from '../Help';

vi.mock('@cratis/components/Common', () => ({
    Button: ({ children }: { children?: ReactNode }) => createElement('button', {}, children),
}));

vi.mock('primereact/popover', () => {
    const content = ({ children }: { children?: ReactNode }) => createElement('div', {}, children);
    return {
        Popover: {
            Root: content,
            Trigger: content,
            Portal: content,
            Positioner: content,
            Popup: content,
            Content: content,
        },
    };
});

describe('when rendering help', () => {
    let markup: string;
    let links: string[];

    beforeEach(() => {
        markup = renderToStaticMarkup(createElement(Help));
        links = markup.match(/<a\b[^>]*>/g) ?? [];
    });

    it('should label the help button', () => {
        markup.should.include(`<button>${strings.layout.topBar.help.title}</button>`);
    });

    it('should link to the Chronicle documentation', () => {
        markup.should.include('href="https://www.cratis.io/chronicle/"');
    });

    it('should label the documentation link', () => {
        markup.should.include(strings.layout.topBar.help.documentation);
    });

    it('should link to the Cratis Discord invite', () => {
        markup.should.include('href="https://discord.gg/kt4AMpV8WV"');
    });

    it('should invite questions on Discord', () => {
        markup.should.include('Questions? Ask on Discord');
    });

    it('should explain how the community can help', () => {
        markup.should.include(strings.layout.topBar.help.communityDescription);
    });

    it('should provide two external links', () => {
        links.should.have.lengthOf(2);
    });

    it('should open external links in a new tab', () => {
        links.forEach(link => link.should.include('target="_blank"'));
    });

    it('should protect external links from opener access', () => {
        links.forEach(link => link.should.include('rel="noopener noreferrer"'));
    });
});
