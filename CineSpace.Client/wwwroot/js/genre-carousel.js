const tracks = new WeakMap();

export function connectGenres(track, reference) {
    const motion = matchMedia('(prefers-reduced-motion: reduce)');
    let frame = 0, last = '';
    const cards = () => [...track.children];
    const maximum = () => Math.max(0, track.scrollWidth - track.clientWidth);
    const publish = () => {
        frame = 0;
        const items = cards(), origin = items[0]?.offsetLeft || 0, left = track.scrollLeft;
        const visible = items.map((item, index) => ({item, index})).filter(({item}) => item.offsetLeft - origin + item.offsetWidth > left + 2 && item.offsetLeft - origin < left + track.clientWidth - 2);
        const values = [visible.length ? visible[0].index + 1 : 0, visible.length ? visible.at(-1).index + 1 : 0, left > 2, left < maximum() - 2, maximum() > 2];
        const key = values.join(':');
        if (last !== key) { last = key; reference.invokeMethodAsync('ActualizarGeneros', ...values).catch(() => {}); }
    };
    const update = () => { if (!frame) frame = requestAnimationFrame(publish); };
    const scrollTo = left => track.scrollTo({left: Math.max(0, Math.min(maximum(), left)), behavior: motion.matches ? 'instant' : 'smooth'});
    const move = direction => {
        const items = cards();
        const stride = items.length > 1 ? items[1].offsetLeft - items[0].offsetLeft : track.clientWidth;
        scrollTo((Math.round(track.scrollLeft / stride) + direction) * stride);
    };
    const keydown = event => {
        if (!['ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(event.key)) return;
        const buttons = [...track.querySelectorAll('.genre-category')], current = buttons.indexOf(event.target);
        if (current < 0) return;
        event.preventDefault();
        const next = event.key === 'Home' ? 0 : event.key === 'End' ? buttons.length - 1 : Math.max(0, Math.min(buttons.length - 1, current + (event.key === 'ArrowRight' ? 1 : -1)));
        buttons[next]?.focus({preventScroll: true});
        scrollTo(cards()[next].offsetLeft - (cards()[0]?.offsetLeft || 0));
    };
    const resize = new ResizeObserver(update), changes = new MutationObserver(update);
    resize.observe(track);
    changes.observe(track, {childList: true});
    track.addEventListener('scroll', update, {passive: true});
    track.addEventListener('keydown', keydown);
    tracks.set(track, {move, dispose: () => {
        resize.disconnect(); changes.disconnect();
        if (frame) cancelAnimationFrame(frame);
        track.removeEventListener('scroll', update); track.removeEventListener('keydown', keydown);
    }});
    publish();
}
export function moveGenres(track, direction) { tracks.get(track)?.move(direction); }
export function disconnectGenres(track) { tracks.get(track)?.dispose(); tracks.delete(track); }
