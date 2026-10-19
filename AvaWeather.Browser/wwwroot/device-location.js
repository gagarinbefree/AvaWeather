let initialPosition;

export function prefetchCurrentPosition() {
    initialPosition ??= requestCurrentPosition();
}

export function getCurrentPosition() {
    const position = initialPosition ?? requestCurrentPosition();
    initialPosition = undefined;
    return position;
}

function requestCurrentPosition() {
    if (!globalThis.isSecureContext || !globalThis.navigator?.geolocation)
        return Promise.resolve(null);

    return new Promise(resolve => {
        try {
            navigator.geolocation.getCurrentPosition(
                position => {
                    const { latitude, longitude } = position.coords;
                    resolve(Number.isFinite(latitude) && Number.isFinite(longitude)
                        ? `${latitude},${longitude}` : null);
                },
                () => resolve(null),
                { enableHighAccuracy: false, timeout: 8000, maximumAge: 60000 }
            );
        } catch {
            resolve(null);
        }
    });
}
