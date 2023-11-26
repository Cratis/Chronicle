// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Button } from 'primereact/button';
import { ColumnFilterApplyTemplateOptions, ColumnFilterClearTemplateOptions, ColumnProps } from 'primereact/column';

const columnFilterClearTemplate = (options: ColumnFilterClearTemplateOptions) => {
    return <Button type="button" icon="pi pi-times" onClick={options.filterClearCallback} severity="secondary"></Button>;
};

const columnFilterApplyTemplate = (options: ColumnFilterApplyTemplateOptions) => {
    return <Button type="button" icon="pi pi-check" onClick={options.filterApplyCallback} severity="success"></Button>;
};

export const ColumnFilterProps: ColumnProps = {
    filter: true,
    filterApply: columnFilterApplyTemplate,
    filterClear: columnFilterClearTemplate
};
