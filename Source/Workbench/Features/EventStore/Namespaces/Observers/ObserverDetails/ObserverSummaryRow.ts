// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Represents a single label/value pair shown in the observer summary table.
 */
export interface ObserverSummaryRow {
    /**
     * The localized label for the row.
     */
    label: string;

    /**
     * The value displayed against the label.
     */
    value: string | number;
}
