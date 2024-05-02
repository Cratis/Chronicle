import { VueDiOptions } from './index';
import { ServiceContainer } from './ServiceContainer';

export type VueDiSetupOptions = VueDiOptions & {
	inject?: any;
};

/**
 * Setup the plugin
 * @param vue
 * @param options
 */
export function setup(vue: any, options?: VueDiSetupOptions) {
	options = { ...options };

	// Create the container
	const container = options.container;
	if (!container)
		throw new Error(`Invalid container when installing vue-di. Did you use { container } from "tsyringe" module?`);
	if (options.providers) {
		for (const provider of options.providers) {
			container.register(provider.token, provider.provider as any);
		}
	}

	// Inject using nuxt if inject is provided
	if (options.inject) {
		options.inject('container', container);
	} else {
        vue.$container = container;
		// Object.defineProperty(vue.prototype, '_container', {
		// 	writable: false,
		// 	enumerable: true,
		// 	value: container,
		// });
	}

	vue.mixin({
		/**
		 * Create the service container
		 */
		beforeCreate() {
			if (!this.$options.services) return;
			const container = new ServiceContainer(vue, this);
            this.$container = vue.$container;
			container.setup();
			//@ts-ignore
			this._serviceContainer = container;
		},
		beforeDestroy() {
			//@ts-ignore
			this._serviceContainer && this._serviceContainer.destroy();
		},
	});
}
