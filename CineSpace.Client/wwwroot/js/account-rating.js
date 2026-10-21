export function observeRating(element) {
    let disposed = false;
    let width = -1;
    function measure() {
        if (disposed || !element?.isConnected) return;
        element.classList.add('is-measuring');
        const overflowing = [...element.querySelectorAll('h2, .account-rating-comment')]
            .some(text => text.scrollHeight > text.clientHeight + 1);
        element.classList.remove('is-measuring');
        element.dataset.overflow = String(overflowing);
    }
    const observer = new ResizeObserver(entries => {
        const nextWidth = entries[0].contentRect.width;
        if (nextWidth === width) return;
        width = nextWidth;
        measure();
    });
    if (element?.isConnected) { observer.observe(element); measure(); }
    const content = new MutationObserver(measure);
    if(element)content.observe(element,{childList:true,characterData:true,subtree:true});
    document.fonts.ready.then(measure);
    return { dispose() { disposed = true; observer.disconnect(); content.disconnect(); } };
}
