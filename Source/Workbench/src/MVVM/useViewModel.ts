// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { container } from "tsyringe";
import { Constructor } from '@aksio/fundamentals';
import { FunctionComponent } from 'react';
import { observer } from 'mobx-react';

export interface IViewContext<T, TProps = any> {
    viewModel: T,
    props: TProps,
}


export function withViewModel<TViewModel, TProps = {}>(viewModelType: Constructor<TViewModel>, view: FunctionComponent<IViewContext<TViewModel, TProps>>) {
    return observer(() => {
        const viewModel = container.resolve<TViewModel>(viewModelType);
        return view({ viewModel, props: undefined! });
    })
}


export const useViewModel = <TViewModel>(viewModelType: Constructor<TViewModel>): TViewModel => {
    return container.resolve(viewModelType);
};
