(function () {
    const form = document.getElementById('booking-form');
    if (!form) return;

    const submitBtn = document.getElementById('submit-btn');
    const startedAtInput = document.getElementById('form_started_at');
    const successEl = document.getElementById('form-success');
    const errorEl = document.getElementById('form-error');

    startedAtInput.value = String(Date.now());

    const referralSource = document.getElementById('referral_source');
    const referralOther = document.getElementById('referral_other');

    const correctionsMode = document.getElementById('corrections_mode');
    const correctionsDetails = document.getElementById('corrections_details');

    const previousMode = document.getElementById('previous_removal_mode');
    const previousDetails = document.getElementById('previous_removal_details');

    const desiredMode = document.getElementById('desired_result_mode');
    const desiredOther = document.getElementById('desired_result_other');

    const phoneInput = document.getElementById('phone');

    function showError(message) {
        errorEl.textContent = message || 'Не удалось отправить заявку. Попробуйте снова.';
        errorEl.classList.remove('hidden');
    }

    function hideAlerts() {
        successEl.classList.add('hidden');
        errorEl.classList.add('hidden');
        errorEl.textContent = '';
    }

    function toggleOptionalInput(selectEl, inputEl, triggerValue) {
        const shouldShow = selectEl.value === triggerValue;
        inputEl.classList.toggle('visible', shouldShow);
        inputEl.required = shouldShow;
        if (!shouldShow) {
            inputEl.value = '';
        }
    }

    function normalizePhoneInput() {
        const digits = phoneInput.value.replace(/\D/g, '').slice(0, 11);
        phoneInput.value = digits;
    }

    referralSource.addEventListener('change', function () {
        toggleOptionalInput(referralSource, referralOther, '__other__');
    });

    correctionsMode.addEventListener('change', function () {
        toggleOptionalInput(correctionsMode, correctionsDetails, '__details__');
    });

    previousMode.addEventListener('change', function () {
        toggleOptionalInput(previousMode, previousDetails, '__details__');
    });

    desiredMode.addEventListener('change', function () {
        toggleOptionalInput(desiredMode, desiredOther, '__other__');
    });

    phoneInput.addEventListener('input', normalizePhoneInput);

    function getTrimmedValue(id) {
        return (document.getElementById(id).value || '').trim();
    }

    form.addEventListener('submit', async function (event) {
        event.preventDefault();
        hideAlerts();

        const referralValue = referralSource.value === '__other__'
            ? getTrimmedValue('referral_other')
            : referralSource.value;

        const correctionsInfo = correctionsMode.value === '__details__'
            ? getTrimmedValue('corrections_details')
            : 'Нет';

        const previousRemovalInfo = previousMode.value === '__details__'
            ? getTrimmedValue('previous_removal_details')
            : 'Нет';

        let previousRemovalWhere = getTrimmedValue('previous_removal_where');
        if (previousMode.value !== '__details__' && !previousRemovalWhere) {
            previousRemovalWhere = 'Не удалял(а)';
            document.getElementById('previous_removal_where').value = previousRemovalWhere;
        }

        const desiredResult = desiredMode.value === '__other__'
            ? getTrimmedValue('desired_result_other')
            : desiredMode.value;

        const payload = {
            full_name: getTrimmedValue('full_name'),
            phone: getTrimmedValue('phone'),
            birth_date: getTrimmedValue('birth_date'),
            address: getTrimmedValue('address'),
            referral_source: referralValue,
            tattoo_type: getTrimmedValue('tattoo_type'),
            tattoo_age: getTrimmedValue('tattoo_age'),
            corrections_info: correctionsInfo,
            previous_removal_info: previousRemovalInfo,
            previous_removal_where: previousRemovalWhere,
            desired_result: desiredResult,
            consent_personal_data: document.getElementById('consent_personal_data').checked,
            form_started_at: Number(startedAtInput.value || Date.now()),
            website: getTrimmedValue('website')
        };

        if (!payload.consent_personal_data) {
            showError('Нужно согласие на обработку персональных данных.');
            return;
        }

        submitBtn.disabled = true;
        submitBtn.textContent = 'Отправляем...';

        try {
            const response = await fetch('/api/public/booking', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(payload)
            });

            if (!response.ok) {
                let message = 'Ошибка при отправке формы';
                try {
                    const errorData = await response.json();
                    if (errorData.detail) {
                        message = Array.isArray(errorData.detail)
                            ? errorData.detail.map((x) => x.msg || x).join('; ')
                            : errorData.detail;
                    }
                } catch (e) {
                    // ignore parsing errors
                }
                throw new Error(message);
            }

            successEl.classList.remove('hidden');
            form.reset();
            startedAtInput.value = String(Date.now());

            toggleOptionalInput(referralSource, referralOther, '__other__');
            toggleOptionalInput(correctionsMode, correctionsDetails, '__details__');
            toggleOptionalInput(previousMode, previousDetails, '__details__');
            toggleOptionalInput(desiredMode, desiredOther, '__other__');

            document.getElementById('previous_removal_where').value = '';
        } catch (error) {
            showError(error.message);
        } finally {
            submitBtn.disabled = false;
            submitBtn.textContent = 'Отправить заявку';
        }
    });
})();
