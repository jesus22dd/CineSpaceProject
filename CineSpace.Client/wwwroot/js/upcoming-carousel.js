const carousels = new WeakMap();
export function connectUpcoming(section, track, reference) {
    if (!(section instanceof Element) || !(track instanceof Element) || !section.isConnected || !track.isConnected) return [];
    disconnectUpcoming(track);
    const motion = matchMedia('(prefers-reduced-motion: reduce)');
    const hover = matchMedia('(hover: hover)');
    const state = { section, track, reference, motion, hover, playing: !motion.matches, visible: false, index: 0, stride: 1, maximum: 0, direction: 1, interaction: 0, frame: null, last: '' };
    const cards = () => [...track.querySelectorAll('.upcoming-card')];
    const measure = () => {
        const list = cards();
        state.stride = list.length > 1 ? list[1].offsetLeft - list[0].offsetLeft : list[0]?.getBoundingClientRect().width || 1;
        state.maximum = Math.max(0, track.scrollWidth - track.clientWidth);
    };
    const publish = () => {
        measure();
        state.index = Math.round(track.scrollLeft / state.stride);
        const gap = parseFloat(getComputedStyle(track).columnGap) || 0;
        const first = Math.min(cards().length, state.index + 1);
        const last = Math.min(cards().length, Math.max(first, Math.floor((track.scrollLeft + track.clientWidth + gap + 1) / state.stride)));
        const key = `${first}:${last}:${state.playing}`;
        if (state.last !== key) { state.last = key; reference.invokeMethodAsync('ActualizarCarrusel', first, last, state.playing).catch(() => {}); }
        return [first, last, state.playing ? 1 : 0];
    };
    state.publish = publish;
    state.move = (direction, manual = true) => {
        measure();
        if (manual) { state.interaction = performance.now(); state.direction = direction; }
        const target = Math.max(0, Math.min(state.maximum, (state.index + direction) * state.stride));
        track.scrollTo({left: target, behavior: motion.matches ? 'instant' : 'smooth'});
    };
    state.scroll = () => {
        if (state.frame) return;
        state.frame = requestAnimationFrame(() => { state.frame = null; publish(); });
    };
    state.keydown = event => {
        if (event.key !== 'ArrowLeft' && event.key !== 'ArrowRight' && event.key !== 'Home' && event.key !== 'End') return;
        event.preventDefault();
        state.playing = false;
        if (event.key === 'Home' || event.key === 'End') {
            measure();
            state.interaction = performance.now();
            track.scrollTo({left: event.key === 'Home' ? 0 : state.maximum, behavior: motion.matches ? 'instant' : 'smooth'});
        } else state.move(event.key === 'ArrowRight' ? 1 : -1);
        publish();
    };
    state.pointerdown = event => { state.interaction = performance.now(); if (event.pointerType === 'touch') { state.playing = false; publish(); } };
    state.motionChanged = () => { if (motion.matches) { state.playing = false; publish(); } };
    state.resize = new ResizeObserver(() => {
        measure();
        track.scrollTo({left: Math.min(state.maximum, state.index * state.stride), behavior: 'instant'});
        publish();
    });
    state.resize.observe(track);
    state.intersection = new IntersectionObserver(entries => { state.visible = entries[0].isIntersecting && entries[0].intersectionRatio >= .2; }, {threshold: .2});
    state.intersection.observe(section);
    state.timer = setInterval(() => {
        if (!state.playing || !state.visible || document.hidden || document.querySelector('dialog[open]') || section.matches(':focus-within') || (hover.matches && section.matches(':hover')) || performance.now() - state.interaction < 3500) return;
        measure();
        if (!state.maximum) return;
        if (track.scrollLeft >= state.maximum - 2) state.direction = -1;
        else if (track.scrollLeft <= 2) state.direction = 1;
        state.move(state.direction, false);
    }, 3500);
    track.addEventListener('scroll', state.scroll, {passive: true});
    track.addEventListener('keydown', state.keydown);
    track.addEventListener('pointerdown', state.pointerdown, {passive: true});
    motion.addEventListener('change', state.motionChanged);
    carousels.set(track, state);
    return publish();
}
export function moveUpcoming(track, direction) { carousels.get(track)?.move(direction); }
export function toggleUpcomingPlayback(track) {
    const state = carousels.get(track);
    if (!state) return;
    state.playing = !state.playing;
    state.interaction = performance.now();
    state.publish();
}
export function disconnectUpcoming(track) {
    const state = carousels.get(track);
    if (!state) return;
    clearInterval(state.timer);
    if (state.frame) cancelAnimationFrame(state.frame);
    state.resize.disconnect();
    state.intersection.disconnect();
    track.removeEventListener('scroll', state.scroll);
    track.removeEventListener('keydown', state.keydown);
    track.removeEventListener('pointerdown', state.pointerdown);
    state.motion.removeEventListener('change', state.motionChanged);
    carousels.delete(track);
}
