 const legacyCacheNamePrefix = 'offline-cache-';

self.addEventListener('install', () => {
    self.skipWaiting();
});

self.addEventListener('activate', event => {
    event.waitUntil(removeLegacyCachesAndClaimClients());
});

// Tyngd kräver nätverk i den här PWA-versionen. Inga appfiler eller API-svar
// lagras av service workern.
self.addEventListener('fetch', event => {
    event.respondWith(fetch(event.request));
});

async function removeLegacyCachesAndClaimClients() {
    const cacheKeys = await caches.keys();
    await Promise.all(cacheKeys
        .filter(key => key.startsWith(legacyCacheNamePrefix))
        .map(key => caches.delete(key)));

    await self.clients.claim();
}

// Tryck på vilotimerns notis: visa appen igen (eller öppna den om den stängts).
self.addEventListener('notificationclick', event => {
    event.notification.close();
    event.waitUntil((async () => {
        const windows = await self.clients.matchAll({ type: 'window', includeUncontrolled: true });
        if (windows.length > 0)
            return windows[0].focus();
        return self.clients.openWindow(self.registration.scope);
    })());
});
