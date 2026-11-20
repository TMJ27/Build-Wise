'use strict';

(() => {
    const provider = document.querySelector('#email-provider');
    const smtp = document.querySelector('#smtp-settings');

    if (!provider || !smtp) return;

    function refreshSmtpFields() {
        smtp.hidden = provider.value !== 'SMTP';
    }

    provider.addEventListener('change', refreshSmtpFields);
    refreshSmtpFields();
})();
