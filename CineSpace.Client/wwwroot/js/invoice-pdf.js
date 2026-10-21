// Generate selectable text and vector rules. Only the app icon and the original QR are images.
window.descargarFacturaPDF = async function (elementId, fileName) {
    const source = document.getElementById(elementId);
    if (!source) throw new Error('El comprobante todavía no está disponible.');
    if (typeof window.html2pdf !== 'function') {
        window.cinePdfLibrary ??= new Promise((resolve, reject) => {
            const script = document.createElement('script');
            script.src = '/vendor/html2pdf/html2pdf.bundle.min.js';
            script.onload = resolve;
            script.onerror = () => { window.cinePdfLibrary = null; script.remove(); reject(new Error('No pudimos cargar el generador PDF. Vuelve a intentar.')); };
            document.head.appendChild(script);
        });
        await window.cinePdfLibrary;
    }
    const canvas = document.createElement('canvas');
    canvas.width = canvas.height = 1;
    const worker = window.html2pdf().set({ canvas, margin:18, jsPDF:{ unit:'mm', format:'a4', orientation:'portrait', compress:true } });
    await worker.toPdf();
    const pdf = await worker.get('pdf');
    const clean = value => String(value ?? '').replace(/\s+/gu, ' ').trim().replace(/·/gu, ' / ').replace(/[–—]/gu, '-');
    const read = selector => clean(source.querySelector(selector)?.textContent);
    const number = read('.invoice-number') || [...source.querySelectorAll('.invoice-identity > p')].find(p => !p.classList.contains('invoice-brand'))?.textContent || '';
    pdf.setProperties({ title:`Factura ${clean(number).replace(/^N\.[º°]\s*/u, '')} | CineSpace`, author:'CineSpace', subject:'Comprobante de compra de entradas', creator:'CineSpace' });
    if (typeof pdf.viewerPreferences === 'function') pdf.viewerPreferences({ DisplayDocTitle:true });

    const left = 18, right = 192, width = right - left, bottom = 270;
    let y = 0;
    function text(value, x, baseline, size = 9.5, bold = false, gray = 35, align = 'left') {
        pdf.setFont('helvetica', bold ? 'bold' : 'normal');
        pdf.setFontSize(size); pdf.setTextColor(gray);
        pdf.text(clean(value), x, baseline, { align });
    }
    function line(top, start = left, end = right, gray = 195, thickness = .25) {
        pdf.setDrawColor(gray); pdf.setLineWidth(thickness); pdf.line(start, top, end, top);
    }
    function wrap(value, maxWidth, size = 9.5, bold = false) {
        pdf.setFont('helvetica', bold ? 'bold' : 'normal'); pdf.setFontSize(size);
        return pdf.splitTextToSize(clean(value), maxWidth);
    }
    function fit(value, x, baseline, maxWidth, size = 9.5, bold = false, align = 'left') {
        pdf.setFont('helvetica', bold ? 'bold' : 'normal');
        let adjusted = size;
        pdf.setFontSize(adjusted);
        while (adjusted > 7 && pdf.getTextWidth(clean(value)) > maxWidth) { adjusted -= .5; pdf.setFontSize(adjusted); }
        text(value, x, baseline, adjusted, bold, 35, align);
    }
    const logo = source.querySelector('.invoice-logo');
    if (logo) await logo.decode();
    function continuation() {
        if (logo) pdf.addImage(logo, 'PNG', left, 16, 8, 8, 'cinespace-logo', 'FAST');
        text('CineSpace', left + (logo ? 11 : 0), 22, 15, true);
        fit(number, right, 22, 95, 8.5, false, 'right');
        line(29); y = 39;
    }
    function reserve(height) {
        if (y + height <= bottom) return false;
        pdf.addPage(); continuation(); return true;
    }

    if (logo) pdf.addImage(logo, 'PNG', left, 17, 10, 10, 'cinespace-logo', 'FAST');
    text('CineSpace', left + (logo ? 13 : 0), 24, 19, true);
    text(read('.invoice-identity h2') || 'Factura de compra', left, 43, 25);
    const numberLines = wrap(number, 123, 9, true);
    const numberHeight = Math.max(9, numberLines.length * 4.5 + 4);
    pdf.setDrawColor(175); pdf.setLineWidth(.25); pdf.rect(left, 49, 127, numberHeight);
    numberLines.forEach((value, i) => text(value, left + 3, 55 + i * 4.5, 9, true));
    const issued = read('.invoice-issued') || clean(source.querySelector('.invoice-identity > p:last-child')?.textContent);
    y = 49 + numberHeight + 6;
    const issuedLines = wrap(issued, 127, 8.5);
    issuedLines.forEach(value => { text(value, left, y, 8.5, false, 100); y += 4; });
    const qr = source.querySelector('.invoice-qr img');
    if (qr) {
        await qr.decode();
        pdf.addImage(qr, 'PNG', 156, 16, 36, 36, 'entry-qr', 'NONE');
        fit(read('.invoice-qr strong'), 174, 58, 39, 9, true, 'center');
        text('QR de ingreso', 174, 63, 7.5, false, 100, 'center');
    }
    y = Math.max(73, y + 5); line(y, left, right, 50, .4); y += 10;

    const sections = [...source.querySelectorAll('.invoice-section')];
    const columns = sections.slice(0, 2).map(section => {
        const rows = [];
        function append(value, bold = false, size = 9.5, gap = 1.8) {
            wrap(value, 81, size, bold).forEach(value => rows.push({ value, bold, size, gap:0 }));
            if (rows.length) rows[rows.length - 1].gap = gap;
        }
        for (const node of section.querySelectorAll(':scope > p, :scope > h4')) append(node.textContent, node.tagName === 'H4' || !!node.querySelector('strong'), node.tagName === 'H4' ? 10.5 : 9.5);
        for (const field of section.querySelectorAll('.invoice-details > div')) append(`${clean(field.querySelector('dt')?.textContent)}: ${clean(field.querySelector('dd')?.textContent)}`, false, 9);
        return { title:clean(section.querySelector('h3')?.textContent).toUpperCase(), rows };
    });
    function columnTitles() { columns.forEach((column, i) => text(column.title, left + i * 93, y, 8, true, 100)); y += 8; }
    columnTitles();
    let dividerStart = y - 11;
    const rowCount = Math.max(0, ...columns.map(column => column.rows.length));
    for (let index = 0; index < rowCount; index++) {
        if (y + 7 > bottom) {
            pdf.setDrawColor(210); pdf.setLineWidth(.2); pdf.line(105, dividerStart, 105, y);
            reserve(7); columnTitles(); dividerStart = y - 11;
        }
        let gap = 0;
        columns.forEach((column, i) => { const row = column.rows[index]; if (row) { text(row.value, left + i * 93, y, row.size, row.bold); gap = Math.max(gap, row.gap); } });
        y += 4.8 + gap;
    }
    pdf.setDrawColor(210); pdf.setLineWidth(.2); pdf.line(105, dividerStart, 105, y - 2);
    y += 9;

    const edges = [left, 100, 120, 156, right];
    const headers = [...source.querySelectorAll('.invoice-table thead th')].map(cell => clean(cell.textContent));
    function tableHeading() {
        reserve(12); pdf.setFillColor(245); pdf.rect(left, y, width, 10, 'F'); line(y, left, right, 110);
        headers.forEach((value, i) => text(value, i === 0 ? left + 3 : edges[i + 1] - 3, y + 6.5, 8, true, 60, i === 0 ? 'left' : 'right'));
        y += 10; line(y, left, right, 150);
    }
    tableHeading();
    for (const row of source.querySelectorAll('.invoice-table tbody tr')) {
        const cells = [...row.querySelectorAll('td')].map(cell => clean(cell.textContent));
        const description = wrap(cells[0], 76, 9.5);
        const rowHeight = Math.max(13, description.length * 5 + 7);
        if (reserve(rowHeight + 1)) tableHeading();
        description.forEach((value, i) => text(value, left + 3, y + 7 + i * 5));
        cells.slice(1, 4).forEach((value, i) => fit(value, edges[i + 2] - 3, y + 7, edges[i + 2] - edges[i + 1] - 6, 9.5, false, 'right'));
        y += rowHeight; line(y);
    }
    y += 10;
    const footer = [...source.querySelectorAll('.invoice-footer p')].map(node => clean(node.textContent));
    const payment = footer[0]?.replace(/^Método de pago:\s*/iu, '') || '';
    const paymentLines = wrap(payment, 77, 9.5);
    const summaryHeight = Math.max(22, 12 + paymentLines.length * 5);
    reserve(summaryHeight + 35);
    pdf.setDrawColor(190); pdf.setLineWidth(.25); pdf.rect(left, y, 84, summaryHeight);
    text('INFORMACIÓN DE PAGO', left + 3, y + 6, 7.5, true, 100);
    paymentLines.forEach((value, i) => text(value, left + 3, y + 13 + i * 5, 9.5));
    pdf.setDrawColor(50); pdf.rect(116, y, 76, summaryHeight);
    text('TOTAL PAGADO', 120, y + 7, 8, true, 90);
    fit(read('.invoice-amount strong'), 188, y + 16, 65, 17, true, 'right');
    y += summaryHeight + 12;
    for (const note of footer.slice(1, -1)) {
        for (const value of wrap(note, width, 8.5)) { reserve(5); text(value, left, y, 8.5, false, 100); y += 4.5; }
        y += 3;
    }
    const count = pdf.getNumberOfPages();
    for (let page = 1; page <= count; page++) {
        pdf.setPage(page); line(279);
        text(footer.at(-1) || 'Gracias por elegir CineSpace.', left, 285, 8.5, false, 100);
        text(`${page} / ${count}`, right, 285, 8, false, 100, 'right');
    }
    pdf.save(fileName);
};
