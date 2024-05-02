
export class ServiceContainer {
    constructor(private readonly vue: any, private readonly vm: any) { }

    /// Setup the services
    setup() {
        this.setupServices();
    }

    /// Cleanup the services
    destroy() { }

    /**
     * Setup the $services variable on the vue vm
     */
    private setupServices() {
        // Create the services
        const services: any = {};
        for (const key in this.vm.$options.services) {
            const serviceClass = this.vm.$options.services[key];
            if (serviceClass) {
                services[key] = this.vm.$container.resolve(serviceClass);
            }
        }
        Object.defineProperty(this.vm, '$services', {
            writable: true,
            enumerable: true,
            value: Object.freeze(services),
        });
    }
}
