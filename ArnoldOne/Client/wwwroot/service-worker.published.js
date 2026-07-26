 const legacyCacheNamePrefix = 'offline-cache-';

self.addEventListener('install', () => {
    self.skipWaiting();
});

self.addEventListener('activate', event => {
    event.waitUntil(removeLegacyCachesAndClaimClients());
});

// Arnold kräver nätverk i den här PWA-versionen. Inga appfiler eller API-svar
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
