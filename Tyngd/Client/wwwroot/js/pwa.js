(function () {
    const updateCheckIntervalMilliseconds = 5 * 60 * 1000;
    const legacyCacheNamePrefix = 'offline-cache-';
    const legacyReloadSessionKey = 'tyngd-pwa-legacy-reloaded';

    let registration = null;
    let deferredInstallPrompt = null;
    let dotNetReference = null;
    let updateAvailable = false;
    let legacyCacheDetected = false;
    let lastUpdateCheck = 0;

    window.tyngd = window.tyngd || {};
    const pwa = window.tyngd.pwa = window.tyngd.pwa || {};

    function isStandalone() {
        return window.matchMedia('(display-mode: standalone)').matches
            || window.navigator.standalone === true;
    }

    function getState() {
        return {
            canInstall: deferredInstallPrompt !== null && !isStandalone(),
            updateAvailable: updateAvailable
        };
    }

    async function notifyStateChanged() {
        if (!dotNetReference) {
            return;
        }

        try {
            await dotNetReference.invokeMethodAsync('OnPwaStateChanged', getState());
        } catch {
            dotNetReference = null;
        }
    }

    async function hasLegacyCache() {
        if (!('caches' in window)) {
            return false;
        }

        const cacheKeys = await caches.keys();
        return cacheKeys.some(key => key.startsWith(legacyCacheNamePrefix));
    }

    async function checkForUpdate() {
        if (!registration) {
            return;
        }

        const now = Date.now();
        if (now - lastUpdateCheck < updateCheckIntervalMilliseconds) {
            return;
        }

        lastUpdateCheck = now;
        try {
            await registration.update();
        } catch {
            // En misslyckad kontroll ska inte störa den öppna appen.
        }
    }

    window.addEventListener('beforeinstallprompt', event => {
        event.preventDefault();
        deferredInstallPrompt = event;
        void notifyStateChanged();
    });

    window.addEventListener('appinstalled', () => {
        deferredInstallPrompt = null;
        void notifyStateChanged();
    });

    document.addEventListener('visibilitychange', () => {
        if (document.visibilityState === 'visible') {
            void checkForUpdate();
        }
    });

    pwa.initialize = async function (reference) {
        dotNetReference = reference;
        const state = getState();
        await notifyStateChanged();
        return state;
    };

    pwa.install = async function () {
        if (!deferredInstallPrompt || isStandalone()) {
            return false;
        }

        const prompt = deferredInstallPrompt;
        deferredInstallPrompt = null;
        await notifyStateChanged();

        await prompt.prompt();
        const choice = await prompt.userChoice;
        return choice.outcome === 'accepted';
    };

    pwa.reload = function () {
        window.location.reload();
    };

    async function registerServiceWorker() {
        if (!('serviceWorker' in navigator)) {
            return;
        }

        legacyCacheDetected = await hasLegacyCache();
        const hadController = navigator.serviceWorker.controller !== null;

        navigator.serviceWorker.addEventListener('controllerchange', () => {
            if (legacyCacheDetected
                && sessionStorage.getItem(legacyReloadSessionKey) !== 'true') {
                sessionStorage.setItem(legacyReloadSessionKey, 'true');
                window.location.reload();
                return;
            }

            if (hadController) {
                updateAvailable = true;
                void notifyStateChanged();
            }
        });

        try {
            registration = await navigator.serviceWorker.register(
                'service-worker.js',
                { updateViaCache: 'none' });
            await checkForUpdate();
        } catch {
            // Tyngd fortsätter som vanlig webbapp om registreringen misslyckas.
        }
    }

    void registerServiceWorker();
})();
