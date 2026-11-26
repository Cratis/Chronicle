// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { withViewModel } from '@cratis/arc.react.mvvm';
import { ComparisonPanel } from './ComparisonPanel';
import { ComparisonProps, ComparisonViewModel } from './ComparisonViewModel';

/**
 * Optional, read-only comparison of the retained revisions of an event already on screen.
 * @param props The event as received and the scope it was read from.
 * @returns The comparison panel.
 */
export const RevisionComparison = withViewModel<ComparisonViewModel, ComparisonProps>(
    ComparisonViewModel,
    ({ viewModel }) => <ComparisonPanel viewModel={viewModel} />);
