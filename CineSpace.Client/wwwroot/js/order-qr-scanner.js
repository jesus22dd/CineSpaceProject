let reader;
const cameras = new WeakMap();
function loadReader() {
    if (!reader) reader = new Promise((resolve, reject) => {
        const script = document.createElement('script'); script.src = new URL('../lib/jsqr/jsQR.js', import.meta.url).href;
        script.onload = () => resolve(window.jsQR); script.onerror = () => { script.remove(); reader = null; reject(new Error('No pudimos cargar el lector QR. Vuelve a intentarlo.')); };
        document.head.appendChild(script);
    });
    return reader;
}
function decode(source, width, height, read) {
    const scale = Math.min(1, 1200 / Math.max(width, height)), canvas = document.createElement('canvas');
    canvas.width = Math.max(1, Math.round(width * scale)); canvas.height = Math.max(1, Math.round(height * scale));
    const context = canvas.getContext('2d', {willReadFrequently: true}); context.drawImage(source, 0, 0, canvas.width, canvas.height);
    const pixels = context.getImageData(0, 0, canvas.width, canvas.height);
    return read(pixels.data, pixels.width, pixels.height, {inversionAttempts: 'attemptBoth'})?.data || null;
}
export async function readImage(dataUrl) {
    const read = await loadReader(), blob = await (await fetch(dataUrl)).blob(), image = await createImageBitmap(blob);
    try { return decode(image, image.width, image.height, read); } finally { image.close(); }
}
export async function startCamera(video, reference) {
    stopCamera(video);
    if (!navigator.mediaDevices?.getUserMedia) throw new Error('La cámara requiere HTTPS o localhost. Puedes leer una imagen QR o introducir el código.');
    const state = {stream: null, timer: null}; cameras.set(video, state);
    try {
        const read = await loadReader();
        if (cameras.get(video) !== state) return;
        const stream = await navigator.mediaDevices.getUserMedia({audio: false, video: {facingMode: {ideal: 'environment'}, width: {ideal: 640}, height: {ideal: 480}}});
        if (cameras.get(video) !== state) { stream.getTracks().forEach(t => t.stop()); return; }
        state.stream = stream; video.srcObject = stream; await video.play();
        state.visibility = () => { if (document.hidden) { stopCamera(video); reference.invokeMethodAsync('CamaraDetenida').catch(() => {}); } };
        document.addEventListener('visibilitychange', state.visibility);
        const scan = () => {
            if (cameras.get(video) !== state) return;
            if (document.hidden) { stopCamera(video); reference.invokeMethodAsync('CamaraDetenida').catch(() => {}); return; }
            if (video.readyState >= 2 && video.videoWidth > 0) {
                const code = decode(video, video.videoWidth, video.videoHeight, read);
                if (code) { stopCamera(video); reference.invokeMethodAsync('CodigoLeido', code).catch(() => {}); return; }
            }
            state.timer = setTimeout(scan, 250);
        };
        scan();
    } catch (error) {
        stopCamera(video);
        if (error.name === 'NotAllowedError') throw new Error('No se permitió acceder a la cámara. Puedes leer una imagen QR o introducir el código.');
        if (error.name === 'NotFoundError') throw new Error('No encontramos una cámara. Puedes usar una imagen QR o introducir el código.');
        if (error.name === 'NotReadableError') throw new Error('La cámara está ocupada. Cierra la otra aplicación o usa una imagen QR.');
        throw error;
    }
}
export function stopCamera(video) {
    const state = cameras.get(video); if (!state) return;
    clearTimeout(state.timer); if (state.visibility) document.removeEventListener('visibilitychange', state.visibility); state.stream?.getTracks().forEach(t => t.stop()); video.srcObject = null; cameras.delete(video);
}
