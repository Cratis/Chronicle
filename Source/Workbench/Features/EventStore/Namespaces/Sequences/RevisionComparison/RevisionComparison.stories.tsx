// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { Meta, StoryObj } from '@storybook/react';
import { AppendedEvent } from 'Features/Sequences';
import { RevisionComparison } from './RevisionComparison';
import { eventWith, revision, scope } from './given/events';

const meta: Meta<typeof RevisionComparison> = {
    title: 'EventStore/Sequences/RevisionComparison',
    component: RevisionComparison,
    tags: ['autodocs'],
    render: (args) => <RevisionComparison {...args} />
};

export default meta;
type Story = StoryObj<typeof RevisionComparison>;

const story = (event: AppendedEvent): Story => ({ args: { scope, event } });

const revised = (...contents: string[]) => eventWith(contents.map((content, index) => revision(content, index + 2, (index + 1) * 5)));

export const QuantityAndStatusChanges: Story = story(revised('{"quantity":2,"status":"new"}', '{"quantity":3,"status":"shipped"}'));
export const ArrayIndexChanges: Story = story(revised('{"lines":["a","b","c"]}', '{"lines":["a","x"]}'));
export const SameRevision: Story = story(revised('{"quantity":2}'));
export const NoHistory: Story = story(eventWith([]));
export const Redacted: Story = story(eventWith([revision('{"quantity":2}', 2, 5)], 'EventRedacted'));
export const DifferingGenerations: Story = story(eventWith([revision('{"quantity":2}', 2, 5), revision('{"quantity":2,"currency":"EUR"}', 5, 10)]));
export const InvalidContent: Story = story(revised('{"quantity":'));
export const LimitedContent: Story = story(revised(`{"deep":${'['.repeat(40)}${']'.repeat(40)}}`));
