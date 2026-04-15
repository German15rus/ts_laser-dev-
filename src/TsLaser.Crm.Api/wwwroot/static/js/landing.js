(function () {
    const form = document.getElementById('booking-form');
    if (!form) return;

    const submitBtn = document.getElementById('submit-btn');
    const startedAtInput = document.getElementById('form_started_at');
    const successEl = document.getElementById('form-success');
    const errorEl = document.getElementById('form-error');

    const referralSource = document.getElementById('referral_source');
    const referralOther = document.getElementById('referral_other');

    const correctionsMode = document.getElementById('corrections_mode');
    const correctionsDetails = document.getElementById('corrections_details');

    const previousMode = document.getElementById('previous_removal_mode');
    const previousDetails = document.getElementById('previous_removal_details');

    const desiredMode = document.getElementById('desired_result_mode');
    const desiredOther = document.getElementById('desired_result_other');

    const phoneInput = document.getElementById('phone');
    const consentInput = document.getElementById('consent_personal_data');
    const consentRow = form.querySelector('.consent-row');

    startedAtInput.value = String(Date.now());

    function showError(message) {
        errorEl.textContent = message || 'Не удалось отправить заявку. Попробуйте снова.';
        errorEl.classList.remove('hidden');
    }

    function hideAlerts() {
        successEl.classList.add('hidden');
        errorEl.classList.add('hidden');
        errorEl.textContent = '';
    }

    function clearAllFieldErrors() {
        form.querySelectorAll('.field-error[data-for]').forEach((node) => node.remove());
        form.querySelectorAll('.input-invalid').forEach((input) => input.classList.remove('input-invalid'));
        if (consentRow) {
            consentRow.classList.remove('consent-invalid');
        }
    }

    function getFieldContainer(inputId) {
        if (inputId === 'consent_personal_data') {
            return consentRow;
        }

        const input = document.getElementById(inputId);
        if (!input) return null;
        return input.closest('.field');
    }

    function setFieldError(inputId, message) {
        const container = getFieldContainer(inputId);
        if (!container) return;

        if (inputId === 'consent_personal_data') {
            let errorNode = form.querySelector('.field-error[data-for="consent_personal_data"]');
            if (!errorNode) {
                errorNode = document.createElement('div');
                errorNode.className = 'field-error';
                errorNode.dataset.for = 'consent_personal_data';
                consentRow.insertAdjacentElement('afterend', errorNode);
            }

            if (consentRow) {
                consentRow.classList.add('consent-invalid');
            }

            errorNode.textContent = message;
            return;
        }

        const input = document.getElementById(inputId);
        if (input) {
            input.classList.add('input-invalid');
        }

        let errorNode = container.querySelector(`.field-error[data-for="${inputId}"]`);
        if (!errorNode) {
            errorNode = document.createElement('div');
            errorNode.className = 'field-error';
            errorNode.dataset.for = inputId;
            container.appendChild(errorNode);
        }

        errorNode.textContent = message;
    }

    function clearFieldError(inputId) {
        const container = getFieldContainer(inputId);
        if (!container) return;

        if (inputId === 'consent_personal_data') {
            const errorNode = form.querySelector('.field-error[data-for="consent_personal_data"]');
            if (errorNode) {
                errorNode.remove();
            }
            if (consentRow) {
                consentRow.classList.remove('consent-invalid');
            }
            return;
        }

        const input = document.getElementById(inputId);
        if (input) {
            input.classList.remove('input-invalid');
        }

        const errorNode = container.querySelector(`.field-error[data-for="${inputId}"]`);
        if (errorNode) {
            errorNode.remove();
        }
    }

    function toggleOptionalInput(selectEl, inputEl, triggerValue) {
        const shouldShow = selectEl.value === triggerValue;
        inputEl.classList.toggle('visible', shouldShow);
        inputEl.required = shouldShow;

        if (!shouldShow) {
            inputEl.value = '';
            clearFieldError(inputEl.id);
        }
    }

    function normalizePhoneInput() {
        const digits = phoneInput.value.replace(/\D/g, '').slice(0, 11);
        phoneInput.value = digits;
        clearFieldError('phone');
    }

    function normalizePhoneForBackend(rawPhone) {
        let digits = (rawPhone || '').replace(/\D/g, '');

        if (digits.length === 11 && (digits[0] === '7' || digits[0] === '8')) {
            digits = digits.slice(1);
        }

        return digits;
    }

    function getTrimmedValue(id) {
        return (document.getElementById(id).value || '').trim();
    }

    function validateLength(value, maxLength) {
        return value.length <= maxLength;
    }

    function validatePayload(payload, state) {
        let isValid = true;

        if (!payload.full_name) {
            setFieldError('full_name', 'Введите ФИО.');
            isValid = false;
        } else if (payload.full_name.length < 2) {
            setFieldError('full_name', 'ФИО должно быть не короче 2 символов.');
            isValid = false;
        } else if (!validateLength(payload.full_name, 255)) {
            setFieldError('full_name', 'Максимум 255 символов.');
            isValid = false;
        }

        if (!payload.phone) {
            setFieldError('phone', 'Введите номер телефона.');
            isValid = false;
        } else if (payload.phone.length !== 10) {
            setFieldError('phone', 'Введите 10 цифр (без +7/8).');
            isValid = false;
        }

        if (!payload.birth_date) {
            setFieldError('birth_date', 'Укажите дату рождения.');
            isValid = false;
        }

        if (!payload.address) {
            setFieldError('address', 'Укажите адрес.');
            isValid = false;
        } else if (!validateLength(payload.address, 500)) {
            setFieldError('address', 'Максимум 500 символов.');
            isValid = false;
        }

        if (!payload.referral_source) {
            setFieldError('referral_source', 'Выберите источник.');
            isValid = false;
        }

        if (state.isReferralOther) {
            const referralOtherValue = getTrimmedValue('referral_other');
            if (!referralOtherValue) {
                setFieldError('referral_other', 'Уточните источник.');
                isValid = false;
            } else if (!validateLength(referralOtherValue, 255)) {
                setFieldError('referral_other', 'Максимум 255 символов.');
                isValid = false;
            }
        }

        if (!payload.tattoo_type) {
            setFieldError('tattoo_type', 'Выберите вариант.');
            isValid = false;
        }

        if (!payload.tattoo_age) {
            setFieldError('tattoo_age', 'Укажите срок.');
            isValid = false;
        } else if (!validateLength(payload.tattoo_age, 255)) {
            setFieldError('tattoo_age', 'Максимум 255 символов.');
            isValid = false;
        }

        if (state.isCorrectionsDetails) {
            const correctionsValue = getTrimmedValue('corrections_details');
            if (!correctionsValue) {
                setFieldError('corrections_details', 'Опишите коррекции/перекрытия.');
                isValid = false;
            } else if (!validateLength(correctionsValue, 500)) {
                setFieldError('corrections_details', 'Максимум 500 символов.');
                isValid = false;
            }
        }

        if (state.isPreviousDetails) {
            const previousValue = getTrimmedValue('previous_removal_details');
            if (!previousValue) {
                setFieldError('previous_removal_details', 'Укажите детали предыдущего удаления.');
                isValid = false;
            } else if (!validateLength(previousValue, 500)) {
                setFieldError('previous_removal_details', 'Максимум 500 символов.');
                isValid = false;
            }
        }

        if (!payload.previous_removal_where) {
            setFieldError('previous_removal_where', 'Укажите, где удаляли ранее.');
            isValid = false;
        } else if (!validateLength(payload.previous_removal_where, 500)) {
            setFieldError('previous_removal_where', 'Максимум 500 символов.');
            isValid = false;
        }

        if (state.isDesiredOther) {
            const desiredOtherValue = getTrimmedValue('desired_result_other');
            if (!desiredOtherValue) {
                setFieldError('desired_result_other', 'Опишите желаемый результат.');
                isValid = false;
            } else if (!validateLength(desiredOtherValue, 500)) {
                setFieldError('desired_result_other', 'Максимум 500 символов.');
                isValid = false;
            }
        }

        if (!payload.desired_result) {
            setFieldError('desired_result_mode', 'Выберите результат.');
            isValid = false;
        }

        if (!payload.consent_personal_data) {
            setFieldError('consent_personal_data', 'Нужно согласие на обработку персональных данных.');
            isValid = false;
        }

        return isValid;
    }

    function applyServerErrorToField(message) {
        const lower = (message || '').toLowerCase();

        if (lower.includes('phone')) {
            setFieldError('phone', 'Введите корректный телефон: 10 цифр без +7/8.');
            return true;
        }

        if (lower.includes('consent')) {
            setFieldError('consent_personal_data', 'Нужно согласие на обработку персональных данных.');
            return true;
        }

        return false;
    }

    referralSource.addEventListener('change', function () {
        toggleOptionalInput(referralSource, referralOther, '__other__');
        clearFieldError('referral_source');
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

    [
        'full_name',
        'phone',
        'birth_date',
        'address',
        'referral_other',
        'tattoo_type',
        'tattoo_age',
        'corrections_details',
        'previous_removal_details',
        'previous_removal_where',
        'desired_result_other'
    ].forEach(function (id) {
        const input = document.getElementById(id);
        if (!input) return;

        input.addEventListener('input', function () {
            clearFieldError(id);
            hideAlerts();
        });

        input.addEventListener('change', function () {
            clearFieldError(id);
        });
    });

    ['referral_source', 'corrections_mode', 'previous_removal_mode', 'desired_result_mode'].forEach(function (id) {
        const input = document.getElementById(id);
        if (!input) return;

        input.addEventListener('change', function () {
            clearFieldError(id);
            hideAlerts();
        });
    });

    consentInput.addEventListener('change', function () {
        clearFieldError('consent_personal_data');
        hideAlerts();
    });

    form.addEventListener('submit', async function (event) {
        event.preventDefault();
        hideAlerts();
        clearAllFieldErrors();

        const isReferralOther = referralSource.value === '__other__';
        const isCorrectionsDetails = correctionsMode.value === '__details__';
        const isPreviousDetails = previousMode.value === '__details__';
        const isDesiredOther = desiredMode.value === '__other__';

        const referralValue = isReferralOther
            ? getTrimmedValue('referral_other')
            : referralSource.value;

        const correctionsInfo = isCorrectionsDetails
            ? getTrimmedValue('corrections_details')
            : 'Нет';

        const previousRemovalInfo = isPreviousDetails
            ? getTrimmedValue('previous_removal_details')
            : 'Нет';

        let previousRemovalWhere = getTrimmedValue('previous_removal_where');
        if (!isPreviousDetails && !previousRemovalWhere) {
            previousRemovalWhere = 'Не удалял(а)';
            document.getElementById('previous_removal_where').value = previousRemovalWhere;
        }

        const desiredResult = isDesiredOther
            ? getTrimmedValue('desired_result_other')
            : desiredMode.value;

        const payload = {
            full_name: getTrimmedValue('full_name'),
            phone: normalizePhoneForBackend(getTrimmedValue('phone')),
            birth_date: getTrimmedValue('birth_date'),
            address: getTrimmedValue('address'),
            referral_source: referralValue,
            tattoo_type: getTrimmedValue('tattoo_type'),
            tattoo_age: getTrimmedValue('tattoo_age'),
            corrections_info: correctionsInfo,
            previous_removal_info: previousRemovalInfo,
            previous_removal_where: previousRemovalWhere,
            desired_result: desiredResult,
            consent_personal_data: consentInput.checked,
            form_started_at: Number(startedAtInput.value || Date.now()),
            website: getTrimmedValue('website')
        };

        const isValid = validatePayload(payload, {
            isReferralOther,
            isCorrectionsDetails,
            isPreviousDetails,
            isDesiredOther
        });

        if (!isValid) {
            showError('Проверьте поля формы.');
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

            window.location.href = '/booking/thank-you';
        } catch (error) {
            const hasFieldError = applyServerErrorToField(error.message);
            if (hasFieldError) {
                showError('Проверьте отмеченные поля.');
            } else {
                showError(error.message);
            }
        } finally {
            submitBtn.disabled = false;
            submitBtn.textContent = 'Отправить заявку';
        }
    });
})();
