'use strict';

document.querySelectorAll('[data-rights-form]').forEach(form => {
    const value = form.querySelector('[data-rights-value]');
    const options = [...form.querySelectorAll('[data-rights-option]')];

    function updateRights() {
        value.value = options.reduce((rights, option) => {
            return option.checked ? rights | Number(option.value) : rights;
        }, 0);
    }

    options.forEach(option => option.addEventListener('change', updateRights));

    form.querySelector('[data-rights-select-all]').addEventListener('click', () => {
        options.forEach(option => { option.checked = true; });
        updateRights();
    });

    form.querySelector('[data-rights-clear]').addEventListener('click', () => {
        options.forEach(option => { option.checked = false; });
        updateRights();
    });

    form.addEventListener('submit', updateRights);
    updateRights();
});
