'use strict';
// All editable date fields use the same calendar and ISO date value for model binding.
jQuery(function ($) {
    $('input[type="date"]').not('[readonly], :disabled').each(function () {
        const input = $(this);
        const value = input.val();
        const minimum = input.attr('min');
        const maximum = input.attr('max');
        input.attr({ type: 'text', placeholder: 'yyyy-mm-dd', autocomplete: 'off' });
        input.val(value).datepicker({
            dateFormat: 'yy-mm-dd',
            changeMonth: true,
            changeYear: true,
            yearRange: 'c-100:c+20',
            minDate: minimum ? $.datepicker.parseDate('yy-mm-dd', minimum) : null,
            maxDate: maximum ? $.datepicker.parseDate('yy-mm-dd', maximum) : null,
            prevText: '‹',
            nextText: '›',
            onSelect: function () {
                this.dispatchEvent(new Event('change', { bubbles: true }));
                if (input.closest('form').data('validator')) input.valid();
            }
        });
    });
});
