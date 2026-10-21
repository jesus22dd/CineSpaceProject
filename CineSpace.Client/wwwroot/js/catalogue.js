const observers = new WeakMap();
export function observeCatalogue(element, reference) {
    if (!(element instanceof Element) || !element.isConnected) return 4;
    let columns = Number(getComputedStyle(element).getPropertyValue('--catalogue-columns')) || 4;
    const observer = new ResizeObserver(() => {
        if (!element.isConnected) { observer.disconnect(); return; }
        const next = Number(getComputedStyle(element).getPropertyValue('--catalogue-columns')) || 4;
        if (next !== columns) {
            columns = next;
            reference.invokeMethodAsync('ActualizarColumnas', columns).catch(() => {});
        }
    });
    observer.observe(element);
    observers.set(element, observer);
    return columns;
}
export function disconnectCatalogue(element) {
    observers.get(element)?.disconnect();
    observers.delete(element);
}
export function focusInfo(element, id) {
    element.querySelector(`[data-movie-id="${id}"] .movie-card-trigger`)?.focus({preventScroll: true});
}
export async function focusCardInformation(element) {
    // Wait for the native dialog and Blazor to finish removing the closed panel.
    await new Promise(requestAnimationFrame);
    if (element.isConnected) element.querySelector('.movie-single-info')?.focus({preventScroll: true});
}
export function focusCatalogueResults(element) {
    const catalogue = element.closest('.catalogue');
    const filters = catalogue?.querySelector('.catalogue-filters');
    const heading = catalogue?.querySelector('#catalogue-results-title');
    if (!filters || !heading) return;
    const headerHeight = document.querySelector('.site-header')?.getBoundingClientRect().height ?? 80;
    window.scrollTo({top: window.scrollY + filters.getBoundingClientRect().top - headerHeight - 20,
        behavior: matchMedia('(prefers-reduced-motion: reduce)').matches ? 'instant' : 'smooth'});
    heading.focus({preventScroll: true});
}
export async function containsFocus(element) {
    // Wait until the browser has moved focus to the next control.
    await new Promise(requestAnimationFrame);
    return element.contains(document.activeElement);
}

const carousels = new WeakMap();
export function observeCarousel(element, reference) {
    const motion = matchMedia('(prefers-reduced-motion: reduce)');
    const hover = matchMedia('(hover: hover)');
    const state = { playing: !motion.matches, visible: true, timer: null, observer: null, motionChanged: null };
    state.observer = new IntersectionObserver(entries => { state.visible = entries[0].isIntersecting; });
    state.observer.observe(element);
    state.motionChanged = () => {
        if (motion.matches) { state.playing = false; reference.invokeMethodAsync('ReducirMovimiento').catch(() => {}); }
    };
    motion.addEventListener('change', state.motionChanged);
    state.motion = motion;
    state.timer = setInterval(() => {
        if (state.playing && state.visible && !document.hidden && !document.querySelector('.auth-modal[open]') && !element.matches(':focus-within') && !(hover.matches && element.matches(':hover'))) {
            reference.invokeMethodAsync('AvanzarAutomaticamente').catch(() => {});
        }
    }, 3500);
    carousels.set(element, state);
    return state.playing;
}
export function setCarouselPlayback(element, playing) {
    const state = carousels.get(element);
    if (state) state.playing = playing;
}
export function disconnectCarousel(element) {
    const state = carousels.get(element);
    if (!state) return;
    clearInterval(state.timer);
    state.observer.disconnect();
    state.motion.removeEventListener('change', state.motionChanged);
    carousels.delete(element);
}

