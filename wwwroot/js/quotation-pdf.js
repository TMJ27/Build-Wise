(() => {
    'use strict';
    const source = document.querySelector('#quotation-pdf-data');
    if (!source) return;
    const data = JSON.parse(source.textContent);
    const filename = `${data.Number}-revision-${data.RevisionNumber}.pdf`;
    const status = document.querySelector('[data-pdf-status]');
    let fonts;
    const money = value => value == null ? 'Not set' : Number(value).toLocaleString('en-US', {
        minimumFractionDigits: 2, maximumFractionDigits: 2
    });
    const date = value => value ? new Date(value.slice(0, 10) + 'T00:00:00').toLocaleDateString('en-GB', {
        day: '2-digit', month: 'short', year: 'numeric'
    }) : 'Not set';

    function report(message) {
        if (status) { status.hidden = false; status.textContent = message; }
    }

    async function font(url) {
        const response = await fetch(url);
        if (!response.ok) throw new Error('Quotation font could not be loaded.');
        const bytes = new Uint8Array(await response.arrayBuffer());
        let binary = '';
        for (let offset = 0; offset < bytes.length; offset += 32768)
            binary += String.fromCharCode(...bytes.subarray(offset, offset + 32768));
        return btoa(binary);
    }

    async function generate() {
        fonts ??= Promise.all([font(source.dataset.fontRegular), font(source.dataset.fontBold)])
            .catch(error => { fonts = null; throw error; });
        const [regular, bold] = await fonts;
        const pdf = new window.jspdf.jsPDF({ unit: 'mm', format: 'a4', compress: true });
        pdf.addFileToVFS('NotoSans-Regular.ttf', regular);
        pdf.addFont('NotoSans-Regular.ttf', 'NotoSans', 'normal');
        pdf.addFileToVFS('NotoSans-Bold.ttf', bold);
        pdf.addFont('NotoSans-Bold.ttf', 'NotoSans', 'bold');
        pdf.setProperties({ title: `${data.Number} - Revision ${data.RevisionNumber}`, author: data.CompanyNameSnapshot });
        let y = 18;
        function text(value, size = 10, weight = 'normal') {
            if (!value) return;
            pdf.setFont('NotoSans', weight);
            pdf.setFontSize(size);
            pdf.setTextColor(23, 41, 66);
            const lines = pdf.splitTextToSize(String(value), 178);
            for (const line of lines) {
                if (y > 272) { pdf.addPage(); y = 18; }
                pdf.text(line, 16, y);
                y += size * 0.45 + 1;
            }
            y += 2;
        }
        // The PDF follows the same two-column document layout as the preview.
        function block(value, x, top, width, size = 9, weight = 'normal', align = 'left') {
            if (!value) return top;
            pdf.setFont('NotoSans', weight);
            pdf.setFontSize(size);
            pdf.setTextColor(23, 41, 66);
            const lines = pdf.splitTextToSize(String(value), width);
            pdf.text(lines, x, top, { align });
            return top + lines.length * size * 0.43 + 2;
        }
        pdf.setFillColor(173, 98, 71);
        pdf.rect(16, 10, 178, 1.2, 'F');
        let companyY = 21;
        let companyX = 16;
        let companyWidth = 86;
        if (data.LogoUrl) {
            try {
                const url = new URL(data.LogoUrl, window.location.href);
                if (url.origin !== window.location.origin) throw new Error('Company logo must be local.');
                const logo = new Image();
                logo.src = url.href;
                await logo.decode();
                const scale = Math.min(22 / logo.naturalWidth, 22 / logo.naturalHeight);
                const canvas = document.createElement('canvas');
                const rasterScale = Math.min(1, 1000 / Math.max(logo.naturalWidth, logo.naturalHeight));
                canvas.width = Math.max(1, Math.round(logo.naturalWidth * rasterScale));
                canvas.height = Math.max(1, Math.round(logo.naturalHeight * rasterScale));
                canvas.getContext('2d').drawImage(logo, 0, 0, canvas.width, canvas.height);
                pdf.addImage(canvas.toDataURL('image/png'), 'PNG', 16, 17,
                    logo.naturalWidth * scale, logo.naturalHeight * scale);
                companyX = 43;
                companyWidth = 59;
            } catch (error) { console.warn('Company logo could not be loaded.', error); }
        }
        companyY = block(data.CompanyNameSnapshot, companyX, companyY, companyWidth, 14, 'bold');
        for (const value of [data.CompanyAddressSnapshot, data.CompanyEmailSnapshot, data.CompanyPhoneSnapshot,
            data.CompanyRegistrationNumberSnapshot && `Registration: ${data.CompanyRegistrationNumberSnapshot}`,
            data.CompanyTaxNumberSnapshot && `Tax number: ${data.CompanyTaxNumberSnapshot}`])
            companyY = block(value, companyX, companyY, companyWidth);
        let referenceY = block('QUOTATION', 194, 21, 78, 16, 'bold', 'right');
        for (const value of [data.Number, `Revision ${data.RevisionNumber}`,
            `Issue date: ${date(data.IssueDate)}`, `Valid until: ${date(data.ValidUntil)}`])
            referenceY = block(value, 194, referenceY, 78, 9, 'normal', 'right');
        y = Math.max(companyY, referenceY) + 5;
        pdf.setDrawColor(225, 231, 238);
        pdf.line(16, y, 194, y);
        y += 10;
        let customerY = block('Prepared for', 16, y, 84, 11, 'bold');
        for (const value of [data.CustomerNameSnapshot, data.CustomerEmailSnapshot,
            data.CustomerPhoneSnapshot, data.CustomerAddressSnapshot])
            customerY = block(value, 16, customerY, 84);
        let projectY = block(data.Title, 112, y, 82, 11, 'bold');
        projectY = block(`Request: ${data.RequestReference || 'Not assigned'}`, 112, projectY, 82);
        projectY = block(data.SiteAddressSnapshot, 112, projectY, 82);
        y = Math.max(customerY, projectY) + 8;
        text('Scope of work', 12, 'bold');
        const headings = ['Service', 'Description', 'Qty', 'Unit'];
        if (data.IncludesPrices) headings.push(`Amount (${data.CurrencyCode})`);
        pdf.autoTable({
            startY: y, margin: { top: 18, bottom: 22, left: 16, right: 16 },
            head: [headings],
            body: data.Items.map(item => {
                const row = [item.Title, item.Description || '—',
                    item.Quantity == null ? '—' : String(item.Quantity), item.UnitCodeSnapshot || '—'];
                if (data.IncludesPrices) row.push(money(item.LineAmount));
                return row;
            }),
            theme: 'grid',
            styles: { font: 'NotoSans', fontSize: 9, cellPadding: 3, textColor: [23, 41, 66], overflow: 'linebreak' },
            headStyles: { fillColor: [240, 243, 248], textColor: [23, 41, 66], fontStyle: 'bold' },
            columnStyles: data.IncludesPrices
                ? { 0: { cellWidth: 38 }, 1: { cellWidth: 72 }, 2: { cellWidth: 14 }, 3: { cellWidth: 22 }, 4: { cellWidth: 32, halign: 'right' } }
                : { 0: { cellWidth: 42 }, 1: { cellWidth: 96 }, 2: { cellWidth: 14 }, 3: { cellWidth: 26 } },
            rowPageBreak: 'avoid'
        });
        y = pdf.lastAutoTable.finalY + 8;
        if (data.IncludesPrices) {
            if (y > 242) { pdf.addPage(); y = 18; }
            pdf.autoTable({
                startY: y, margin: { left: 110, right: 16, bottom: 22 }, tableWidth: 84,
                body: [['Subtotal', money(data.Subtotal)], ['Tax', money(data.TaxAmount)],
                    [`Total (${data.CurrencyCode})`, money(data.Total)]],
                theme: 'plain', styles: { font: 'NotoSans', fontSize: 10, cellPadding: 3, textColor: [23, 41, 66] },
                columnStyles: { 1: { halign: 'right' } },
                didParseCell: cell => {
                    if (cell.row.index === 2) { cell.cell.styles.fillColor = [240, 243, 248]; cell.cell.styles.fontStyle = 'bold'; }
                }
            });
            y = pdf.lastAutoTable.finalY + 10;
        }
        text('Terms', 11, 'bold');
        text(data.Terms || 'Not provided');
        text('Assumptions and exclusions', 11, 'bold');
        text(data.Exclusions || 'Not provided');
        for (let page = 1; page <= pdf.getNumberOfPages(); page++) {
            pdf.setPage(page);
            pdf.setFont('NotoSans', 'normal');
            pdf.setFontSize(8);
            pdf.text(`${data.Number} | Revision ${data.RevisionNumber} | Page ${page} of ${pdf.getNumberOfPages()}`, 105, 286, { align: 'center' });
        }
        return pdf;
    }

    document.querySelector('#download-quotation')?.addEventListener('click', async event => {
        const button = event.currentTarget;
        button.disabled = true;
        report('Preparing PDF…');
        try { (await generate()).save(filename); report('PDF downloaded.'); }
        catch (error) { console.error(error); report('PDF could not be generated. Please try again.'); }
        finally { button.disabled = false; }
    });

    const form = document.querySelector('[data-send-quotation]');
    let prepared = false;
    let generating = false;
    form?.addEventListener('submit', async event => {
        if (prepared || event.defaultPrevented) return;
        event.preventDefault();
        if (generating || (window.jQuery?.fn.valid && !window.jQuery(form).valid())) return;
        generating = true;
        const button = form.querySelector('[type="submit"]');
        button.disabled = true;
        report('Preparing quotation PDF…');
        try {
            const pdf = await generate();
            const files = new DataTransfer();
            files.items.add(new File([pdf.output('blob')], filename, { type: 'application/pdf' }));
            form.querySelector('[data-pdf-attachment]').files = files.files;
            prepared = true;
            button.disabled = false;
            report('Sending quotation…');
            // Start a new task so the original submit event has finished dispatching.
            setTimeout(() => form.requestSubmit(button), 0);
        } catch (error) {
            console.error(error);
            report('The quotation PDF could not be prepared. Please try again.');
            button.disabled = false;
            generating = false;
        }
    });
})();
