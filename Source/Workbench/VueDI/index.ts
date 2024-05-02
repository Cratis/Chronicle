import { Plugin } from 'vue';
import {
	InjectionToken,
	ValueProvider,
	FactoryProvider,
	TokenProvider,
	ClassProvider,
	DependencyContainer,
} from 'tsyringe';
import { setup } from './setup';

export type VueDiProvider = {
	token: InjectionToken;
	provider:
		| ValueProvider<any>
		| FactoryProvider<any>
		| TokenProvider<any>
		| ClassProvider<any>;
};

export type VueDiOptions = {
	inject?: (name: string, container: DependencyContainer) => void;
	container?: DependencyContainer;
	providers?: VueDiProvider[];
};

const Plugin: Plugin<VueDiOptions> = {
	install(vue: any, options?: VueDiOptions) {
		setup(vue, options);
	},
};
export default Plugin;
export { setup };
