// Load PDF generation only when requested. Text and rules are vector graphics;
// the QR retains its original PNG, so printing never rasterizes the receipt.
window.descargarFacturaPDF = async function (elementId, fileName) {
    if (typeof window.html2pdf !== 'function') {
        window.cinePdfLibrary ??= new Promise((resolve,reject) => {
            const script=document.createElement('script'); script.src='/vendor/html2pdf/html2pdf.bundle.min.js';
            script.onload=resolve; script.onerror=()=> { window.cinePdfLibrary=null; script.remove(); reject(new Error('No pudimos cargar el generador PDF. Vuelve a intentar.')); };
            document.head.appendChild(script);
        });
        await window.cinePdfLibrary;
    }
    const source=document.getElementById(elementId);
    if (!source) throw new Error('El comprobante todavía no está disponible.');
    const canvas=document.createElement('canvas'); canvas.width=canvas.height=1;
    // The bundled worker exposes its jsPDF instance; no HTML screenshot is taken.
    const worker=window.html2pdf().set({canvas,margin:16,jsPDF:{unit:'mm',format:'a4',orientation:'portrait',compress:true}});
    await worker.toPdf(); const pdf=await worker.get('pdf');
    pdf.setProperties({title:'Comprobante de compra - CineSpace',author:'CineSpace',subject:'Entradas de cine'});
    const clean=t=>String(t||'').replace(/\s+/g,' ').trim().replace(/[·]/g,' / ');
    const text=(t,x,y,size=10,bold=false)=> { pdf.setFont('helvetica',bold?'bold':'normal'); pdf.setFontSize(size);pdf.setTextColor(28);pdf.text(clean(t),x,y); };
    const rule=(y,width=.3)=> { pdf.setDrawColor(60);pdf.setLineWidth(width);pdf.line(16,y,194,y); };
    const identity=source.querySelector('.invoice-identity');
    text('CineSpace',16,24,21,true);text('Comprobante de compra',16,34,13,true);
    [...identity.querySelectorAll('p')].filter(p=>!p.classList.contains('invoice-brand')).forEach((p,i)=>text(p.textContent,16,43+i*6,9));
    const qr=source.querySelector('.invoice-qr img');
    if(qr){await qr.decode();pdf.addImage(qr.src,'PNG',157,16,36,36,undefined,'NONE');text(source.querySelector('.invoice-qr strong')?.textContent,160,58,10,true);}
    rule(65,.55);
    let y=74;
    for(const section of source.querySelectorAll('.invoice-section')){
        text(section.querySelector('h3')?.textContent.toUpperCase(),16,y,9,true);y+=8;
        for(const p of section.querySelectorAll(':scope > p,:scope > h4')){
            pdf.setFontSize(p.tagName==='H4'?13:10);const lines=pdf.splitTextToSize(clean(p.textContent),174);
            lines.forEach(line=>{text(line,16,y,p.tagName==='H4'?13:10,p.tagName==='H4'||!!p.querySelector('strong'));y+=6;});
        }
        const fields=[...section.querySelectorAll('.invoice-details > div')];
        for(let i=0;i<fields.length;i+=2){
            let height=0;
            for(const [offset,field] of fields.slice(i,i+2).entries()){
                const x=16+offset*90;text(field.querySelector('dt')?.textContent,x,y,8);
                pdf.setFontSize(10);const lines=pdf.splitTextToSize(clean(field.querySelector('dd')?.textContent),84);
                lines.forEach((line,n)=>text(line,x,y+6+n*5,10));height=Math.max(height,11+lines.length*5);
            }
            y+=height;
        }
        y+=3;rule(y);y+=9;
    }
    const xs=[16,108,132,167];
    source.querySelectorAll('.invoice-table thead th').forEach((cell,i)=>text(cell.textContent,xs[i],y,8,true));
    y+=4;rule(y,.45);y+=8;
    source.querySelectorAll('.invoice-table tbody tr').forEach(row=>{row.querySelectorAll('td').forEach((cell,i)=>text(cell.textContent,xs[i],y,9));y+=8;});
    rule(y);y+=11;
    text('Total pagado',16,y,11,true);
    pdf.setFont('helvetica','bold');pdf.setFontSize(17);pdf.text(clean(source.querySelector('.invoice-amount strong')?.textContent),194,y,{align:'right'});
    y+=7;rule(y,.55);y+=9;
    source.querySelectorAll('.invoice-footer p').forEach(p=>{
        pdf.setFontSize(9);const lines=pdf.splitTextToSize(clean(p.textContent),178);
        lines.forEach(line=>{text(line,16,y,9);y+=5;});y+=2;
    });
    pdf.save(fileName);
};
