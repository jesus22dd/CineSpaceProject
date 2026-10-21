export function observeRating(element) {
    const source = element?.querySelector('.account-rating-source');
    const preview = element?.querySelector('.account-rating-preview');
    const paragraph = element?.querySelector('.account-rating-comment');
    const more = element?.querySelector('.account-rating-more');
    const heading = element?.querySelector('h2');
    if (!source || !preview || !paragraph || !more) return { dispose() {} };

    let disposed = false;
    let width = -1;
    let frame = 0;
    function fits() {
        // The enlarged touch target is outside the line box; measure only visible content.
        // Local offsets also avoid viewport scroll anchoring during text measurement.
        return preview.offsetHeight <= paragraph.clientHeight + 1 &&
            preview.offsetWidth <= paragraph.clientWidth + 1 &&
            more.offsetTop + more.offsetHeight <= paragraph.clientHeight + 1 &&
            more.offsetLeft + more.offsetWidth <= paragraph.clientWidth + 1;
    }
    function measure() {
        frame = 0;
        if (disposed || !element.isConnected) return;
        const text = source.textContent.replace(/\s+/gu, ' ').trim();
        element.dataset.overflow = 'false';
        preview.textContent = text;
        const truncated = !fits();
        if (!truncated && heading.scrollWidth <= heading.clientWidth + 1) return;
        element.dataset.overflow = 'true';
        preview.textContent = text;
        if (fits()) return;

        // Reserve the inline button while measuring the final visible line.
        const characters = Array.from(text);
        let low = 0;
        let high = characters.length;
        while (low < high) {
            const middle = Math.ceil((low + high) / 2);
            preview.textContent = characters.slice(0, middle).join('').trimEnd();
            if (fits()) low = middle; else high = middle - 1;
        }
        let excerpt = characters.slice(0, low).join('');
        const word = excerpt.search(/\s+\S*$/u);
        if (word > excerpt.length * .7 && /\S/u.test(characters[low] ?? '')) excerpt = excerpt.slice(0, word);
        preview.textContent = excerpt.trimEnd();
    }
    function schedule() {
        if (!disposed && !frame) frame = requestAnimationFrame(measure);
    }
    const observer = new ResizeObserver(() => {
        const nextWidth = paragraph.clientWidth;
        if (nextWidth === width) return;
        width = nextWidth;
        schedule();
    });
    observer.observe(paragraph);
    const content = new MutationObserver(schedule);
    content.observe(source, { childList:true, characterData:true, subtree:true });
    content.observe(heading, { childList:true, characterData:true, subtree:true });
    document.fonts.ready.then(schedule);
    document.fonts.addEventListener('loadingdone', schedule);
    schedule();
    return { dispose() { disposed = true; cancelAnimationFrame(frame); observer.disconnect(); content.disconnect(); document.fonts.removeEventListener('loadingdone', schedule); } };
}
