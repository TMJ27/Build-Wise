(() => {
    for (const customerType of document.querySelectorAll('[data-client-type]')) {
        const labels = [...document.querySelectorAll(`[data-client-label="${customerType.id}"]`)];

        function updateCustomerLabels() {
            const type = customerType.value;
            for (const label of labels) {
                if (type === '1') {
                    label.textContent = label.dataset.individual;
                } else if (type === '2') {
                    label.textContent = label.dataset.business;
                } else {
                    label.textContent = label.dataset.default;
                }
            }
        }

        for (const label of labels) {
            label.dataset.default = label.textContent.trim();
        }

        customerType.addEventListener('change', updateCustomerLabels);
        updateCustomerLabels();
    }
})();
