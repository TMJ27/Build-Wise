'use strict';

(() => {
    const form = document.querySelector('#business-profile-form');

    if (!form) return;

    const logoInput = form.querySelector('input[type="file"]');
    const removeLogo = form.querySelector('input[type="checkbox"][name="RemoveLogo"]');
    const images = document.querySelectorAll('.configuration-logo-image');
    const initials = document.querySelectorAll('.configuration-initials');
    const originalLogo = images[0]?.getAttribute('src') || '';
    const logoError = document.querySelector('#logo-client-error');
    let uploadedLogo = '';
    let readVersion = 0;

    function refreshLogo() {
        const source = removeLogo?.checked ? '' : uploadedLogo || originalLogo;

        images.forEach(image => {
            image.hidden = !source;
            if (source) image.src = source;
            else image.removeAttribute('src');
        });

        initials.forEach(element => { element.hidden = Boolean(source); });
    }

    function refreshText() {
        form.querySelectorAll('[data-preview]').forEach(input => {
            document.querySelectorAll(`[data-preview-output="${input.dataset.preview}"]`).forEach(output => {
                output.textContent = input.value;
            });
        });

        const name = form.querySelector('[data-preview="name"]')?.value.trim() || '';
        const letters = name.split(/\s+/).filter(Boolean).slice(0, 2).map(part => part[0].toUpperCase()).join('');
        initials.forEach(element => { element.textContent = letters; });
    }

    form.addEventListener('input', refreshText);
    removeLogo?.addEventListener('change', refreshLogo);

    logoInput?.addEventListener('change', () => {
        const currentRead = ++readVersion;
        const file = logoInput.files?.[0];
        uploadedLogo = '';
        logoError.textContent = '';

        if (!file) {
            refreshLogo();
            return;
        }

        if (!['image/png', 'image/jpeg'].includes(file.type) || file.size > 2 * 1024 * 1024) {
            logoError.textContent = 'Choose a PNG or JPEG image up to 2 MB.';
            logoInput.value = '';
            refreshLogo();
            return;
        }

        if (removeLogo) removeLogo.checked = false;

        const reader = new FileReader();
        reader.addEventListener('load', () => {
            if (currentRead !== readVersion) return;
            uploadedLogo = String(reader.result);
            refreshLogo();
        });
        reader.readAsDataURL(file);
    });

    form.addEventListener('reset', () => {
        readVersion++;
        uploadedLogo = '';
        logoError.textContent = '';

        setTimeout(() => {
            refreshText();
            refreshLogo();
        }, 0);
    });

    refreshText();
    refreshLogo();
})();
