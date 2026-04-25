/* TS Laser CRM - Calendar */

'use strict';

// ── Constants ──────────────────────────────────────────────────────────────
const WORKING_DAYS = [2, 4, 6]; // Tue=2, Thu=4, Sat=6
const WORK_START = 10;
const WORK_END = 20;
const HOURS = Array.from({ length: WORK_END - WORK_START + 1 }, (_, i) => WORK_START + i);
const DAY_NAMES = ['Вс', 'Пн', 'Вт', 'Ср', 'Чт', 'Пт', 'Сб'];
const MONTHS = ['янв', 'фев', 'мар', 'апр', 'май', 'июн', 'июл', 'авг', 'сен', 'окт', 'ноя', 'дек'];

const STATUS_LABELS = {
    waiting: 'В ожидании',
    in_progress: 'В работе',
    completed: 'Завершен',
};

const DAY_FULL_NAMES = ['Воскресенье', 'Понедельник', 'Вторник', 'Среда', 'Четверг', 'Пятница', 'Суббота'];

// ── State ──────────────────────────────────────────────────────────────────
let currentWeekStart = getMonday(new Date());
let appointments = [];
let availableClients = [];
let editingAppointmentId = null;
let createTargetDate = null;
let createTargetHour = null;
let currentView = 'calendar';

// ── Init ───────────────────────────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', () => {
    loadWeek();
});

// ── Week navigation ────────────────────────────────────────────────────────
function prevWeek() {
    currentWeekStart = new Date(currentWeekStart);
    currentWeekStart.setDate(currentWeekStart.getDate() - 7);
    loadWeek();
}

function nextWeek() {
    currentWeekStart = new Date(currentWeekStart);
    currentWeekStart.setDate(currentWeekStart.getDate() + 7);
    loadWeek();
}

function goToday() {
    currentWeekStart = getMonday(new Date());
    loadWeek();
}

// ── Data loading ───────────────────────────────────────────────────────────
async function loadWeek() {
    const weekEnd = new Date(currentWeekStart);
    weekEnd.setDate(weekEnd.getDate() + 6);

    const from = formatDateParam(currentWeekStart);
    const to = formatDateParam(weekEnd);

    try {
        const res = await fetch(`/api/calendar/appointments?from=${from}&to=${to}`);
        if (!res.ok) {
            if (res.status === 401) { window.location.href = '/'; return; }
            throw new Error('Ошибка загрузки');
        }
        appointments = await res.json();
    } catch (e) {
        appointments = [];
    }

    if (currentView === 'calendar') {
        renderGrid();
    } else {
        renderSchedule();
    }
    updateWeekLabel();
}

// ── Grid rendering ─────────────────────────────────────────────────────────
function renderGrid() {
    const grid = document.getElementById('cal-grid');
    grid.innerHTML = '';

    const days = getWeekDays(currentWeekStart);
    const today = new Date();

    // Header: empty corner + 7 day headers
    const corner = document.createElement('div');
    corner.className = 'cal-time-label';
    corner.style.borderBottom = '2px solid #000';
    corner.style.borderRight = '2px solid #000';
    grid.appendChild(corner);

    days.forEach(day => {
        const isWork = WORKING_DAYS.includes(day.getDay());
        const isToday = isSameDay(day, today);
        const cell = document.createElement('div');
        cell.className = `cal-header-cell ${isWork ? 'cal-day-work' : 'cal-day-off'}${isToday ? ' cal-today' : ''}`;
        cell.innerHTML = `<div class="cal-header-day">${DAY_NAMES[day.getDay()]}</div><span class="cal-header-date">${day.getDate()} ${MONTHS[day.getMonth()]}</span>`;
        grid.appendChild(cell);
    });

    // Rows: one per hour
    HOURS.forEach((hour, rowIdx) => {
        const isLastRow = rowIdx === HOURS.length - 1;

        // Time label
        const timeLabel = document.createElement('div');
        timeLabel.className = 'cal-time-label';
        timeLabel.textContent = `${hour}:00`;
        if (isLastRow) timeLabel.style.borderBottom = 'none';
        grid.appendChild(timeLabel);

        // Day cells
        days.forEach(day => {
            const isWork = WORKING_DAYS.includes(day.getDay());
            const cell = document.createElement('div');
            cell.className = `cal-cell ${isWork ? 'cal-cell-work' : 'cal-cell-off'}`;
            if (isLastRow) cell.style.borderBottom = 'none';

            if (isWork) {
                const cellAppts = getCellAppointments(day, hour);
                if (cellAppts.length > 0) {
                    cell.appendChild(buildAppointmentsList(cellAppts));
                }
                if (hour < WORK_END) {
                    cell.addEventListener('click', (e) => {
                        if (e.target.closest('.cal-appt-row')) return;
                        openCreateModal(day, hour);
                    });
                }
            }

            grid.appendChild(cell);
        });
    });
}

function buildAppointmentsList(appts) {
    const container = document.createElement('div');
    container.className = 'cal-appointments';

    appts.forEach(appt => {
        const row = document.createElement('div');
        row.className = 'cal-appt-row' + (appt.appointment_status === 'completed' ? ' cal-appt-row--completed' : '');
        row.title = `${appt.client_name} | ${appt.service || '—'} | до ${formatTime(appt.end_time)} | ${appt.master_name}`;

        const badgeClass = `cal-badge-${appt.appointment_status}`;
        const statusLabel = STATUS_LABELS[appt.appointment_status] || appt.appointment_status;

        row.innerHTML = `
            <span class="cal-appt-name">${escHtml(appt.client_name)}</span>
            <span class="cal-appt-service">${escHtml(appt.service || '—')}</span>
            <span class="cal-badge ${badgeClass}">${escHtml(statusLabel)}</span>
            <span class="cal-appt-master">${escHtml(appt.master_name)}</span>
        `;

        row.addEventListener('click', (e) => {
            e.stopPropagation();
            openEditModal(appt);
        });

        container.appendChild(row);
    });

    return container;
}

function getCellAppointments(day, hour) {
    return appointments.filter(appt => {
        const apptDate = new Date(appt.start_time);
        return isSameDay(apptDate, day) && apptDate.getHours() === hour;
    });
}

// ── Create Modal ───────────────────────────────────────────────────────────
async function openCreateModal(day, hour) {
    createTargetDate = day;
    createTargetHour = hour;
    editingAppointmentId = null;

    const dt = new Date(day);
    dt.setHours(hour, 0, 0, 0);
    document.getElementById('create-datetime').value = toDatetimeLocal(dt);
    document.getElementById('create-master').value = '';
    document.getElementById('create-hours').value = '';
    document.getElementById('create-minutes').value = '';
    document.getElementById('create-endtime-preview').classList.add('hidden');
    document.getElementById('create-duration-error').classList.add('hidden');
    document.getElementById('create-submit-btn').disabled = false;

    const clientSelect = document.getElementById('create-client');
    clientSelect.innerHTML = '<option value="">Загрузка...</option>';
    document.getElementById('create-no-clients').classList.add('hidden');

    document.getElementById('modal-create').classList.remove('hidden');

    try {
        const res = await fetch('/api/calendar/available-clients');
        if (!res.ok) throw new Error();
        availableClients = await res.json();
    } catch {
        availableClients = [];
    }

    clientSelect.innerHTML = '';
    if (availableClients.length === 0) {
        document.getElementById('create-no-clients').classList.remove('hidden');
        clientSelect.innerHTML = '<option value="">Нет доступных клиентов</option>';
        document.getElementById('create-submit-btn').disabled = true;
        return;
    }

    const placeholder = document.createElement('option');
    placeholder.value = '';
    placeholder.textContent = '— Выберите клиента —';
    clientSelect.appendChild(placeholder);

    availableClients.forEach(c => {
        const opt = document.createElement('option');
        opt.value = c.submission_id;
        opt.textContent = `${c.full_name}${c.tattoo_type ? ` (${c.tattoo_type})` : ''} — ${c.phone}`;
        clientSelect.appendChild(opt);
    });
}

function closeCreateModal(e) {
    if (e && e.target !== document.getElementById('modal-create')) return;
    document.getElementById('modal-create').classList.add('hidden');
}

function onCreateTimeChange() {
    validateDurationPreview('create');
}

async function submitCreate() {
    const datetimeVal = document.getElementById('create-datetime').value;
    const clientId = parseInt(document.getElementById('create-client').value, 10);
    const masterName = document.getElementById('create-master').value.trim();
    const hours = parseInt(document.getElementById('create-hours').value, 10) || 0;
    const minutes = parseInt(document.getElementById('create-minutes').value, 10) || 0;
    const durationMinutes = hours * 60 + minutes;

    if (!datetimeVal) { alert('Укажите дату и время'); return; }
    if (!clientId) { alert('Выберите клиента'); return; }
    if (!masterName) { alert('Укажите имя мастера'); return; }
    if (durationMinutes < 1) { alert('Укажите длительность'); return; }

    const startTime = new Date(datetimeVal);
    const endTime = new Date(startTime.getTime() + durationMinutes * 60000);
    const workEnd = new Date(startTime);
    workEnd.setHours(WORK_END, 0, 0, 0);

    if (endTime > workEnd) {
        document.getElementById('create-duration-error').textContent =
            `Сеанс заканчивается в ${formatTime(endTime.toISOString())}, рабочий день до ${WORK_END}:00`;
        document.getElementById('create-duration-error').classList.remove('hidden');
        return;
    }

    try {
        const res = await fetch('/api/calendar/appointments', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                intake_submission_id: clientId,
                master_name: masterName,
                start_time: startTime.toISOString(),
                duration_minutes: durationMinutes,
            }),
        });

        if (!res.ok) {
            const err = await res.json().catch(() => ({ detail: 'Ошибка' }));
            alert(err.detail || 'Ошибка создания записи');
            return;
        }

        document.getElementById('modal-create').classList.add('hidden');
        loadWeek();
    } catch {
        alert('Ошибка сети');
    }
}

// ── Edit Modal ─────────────────────────────────────────────────────────────
function openEditModal(appt) {
    editingAppointmentId = appt.id;

    document.getElementById('edit-client-info').textContent = appt.client_name;
    document.getElementById('edit-service-info').textContent = appt.service || '—';

    const startDate = new Date(appt.start_time);
    document.getElementById('edit-datetime').value = toDatetimeLocal(startDate);

    document.getElementById('edit-master').value = appt.master_name;

    const h = Math.floor(appt.duration_minutes / 60);
    const m = appt.duration_minutes % 60;
    document.getElementById('edit-hours').value = h;
    document.getElementById('edit-minutes').value = m;

    document.getElementById('edit-status').value = appt.appointment_status;

    document.getElementById('edit-endtime-preview').classList.add('hidden');
    document.getElementById('edit-duration-error').classList.add('hidden');
    document.getElementById('edit-submit-btn').disabled = false;

    onEditTimeChange();

    const isCompleted = appt.appointment_status === 'completed';
    ['edit-datetime', 'edit-master', 'edit-hours', 'edit-minutes', 'edit-status'].forEach(id => {
        document.getElementById(id).disabled = isCompleted;
    });
    document.getElementById('edit-delete-btn').classList.toggle('hidden', isCompleted);
    document.getElementById('edit-submit-btn').classList.toggle('hidden', isCompleted);

    document.getElementById('modal-edit').classList.remove('hidden');
}

function closeEditModal(e) {
    if (e && e.target !== document.getElementById('modal-edit')) return;
    document.getElementById('modal-edit').classList.add('hidden');
}

function onEditTimeChange() {
    validateDurationPreview('edit');
}

async function submitEdit() {
    if (!editingAppointmentId) return;

    const datetimeVal = document.getElementById('edit-datetime').value;
    const masterName = document.getElementById('edit-master').value.trim();
    const hours = parseInt(document.getElementById('edit-hours').value, 10) || 0;
    const minutes = parseInt(document.getElementById('edit-minutes').value, 10) || 0;
    const durationMinutes = hours * 60 + minutes;
    const status = document.getElementById('edit-status').value;

    if (!datetimeVal) { alert('Укажите дату и время'); return; }
    if (!masterName) { alert('Укажите имя мастера'); return; }
    if (durationMinutes < 1) { alert('Укажите длительность'); return; }

    const startTime = new Date(datetimeVal);
    const endTime = new Date(startTime.getTime() + durationMinutes * 60000);
    const workEnd = new Date(startTime);
    workEnd.setHours(WORK_END, 0, 0, 0);

    if (endTime > workEnd) {
        document.getElementById('edit-duration-error').textContent =
            `Сеанс заканчивается в ${formatTime(endTime.toISOString())}, рабочий день до ${WORK_END}:00`;
        document.getElementById('edit-duration-error').classList.remove('hidden');
        return;
    }

    try {
        const res = await fetch(`/api/calendar/appointments/${editingAppointmentId}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                start_time: startTime.toISOString(),
                master_name: masterName,
                duration_minutes: durationMinutes,
                appointment_status: status,
            }),
        });

        if (!res.ok) {
            const err = await res.json().catch(() => ({ detail: 'Ошибка' }));
            alert(err.detail || 'Ошибка обновления');
            return;
        }

        document.getElementById('modal-edit').classList.add('hidden');
        loadWeek();
    } catch {
        alert('Ошибка сети');
    }
}

async function deleteAppointment() {
    if (!editingAppointmentId) return;
    if (!confirm('Удалить запись? Клиент вернётся в список доступных.')) return;

    try {
        const res = await fetch(`/api/calendar/appointments/${editingAppointmentId}`, {
            method: 'DELETE',
        });

        if (!res.ok && res.status !== 204) {
            const err = await res.json().catch(() => ({ detail: 'Ошибка' }));
            alert(err.detail || 'Ошибка удаления');
            return;
        }

        document.getElementById('modal-edit').classList.add('hidden');
        loadWeek();
    } catch {
        alert('Ошибка сети');
    }
}

// ── Duration validation helper ─────────────────────────────────────────────
function validateDurationPreview(prefix) {
    const datetimeVal = document.getElementById(`${prefix}-datetime`).value;
    const hours = parseInt(document.getElementById(`${prefix}-hours`).value, 10) || 0;
    const minutes = parseInt(document.getElementById(`${prefix}-minutes`).value, 10) || 0;
    const durationMinutes = hours * 60 + minutes;

    const previewEl = document.getElementById(`${prefix}-endtime-preview`);
    const errorEl = document.getElementById(`${prefix}-duration-error`);
    const submitBtn = document.getElementById(`${prefix}-submit-btn`);

    if (!datetimeVal || durationMinutes < 1) {
        previewEl.classList.add('hidden');
        errorEl.classList.add('hidden');
        return;
    }

    const startTime = new Date(datetimeVal);
    const endTime = new Date(startTime.getTime() + durationMinutes * 60000);
    const workEnd = new Date(startTime);
    workEnd.setHours(WORK_END, 0, 0, 0);

    const endHH = String(endTime.getHours()).padStart(2, '0');
    const endMM = String(endTime.getMinutes()).padStart(2, '0');

    if (endTime > workEnd) {
        errorEl.textContent = `Сеанс заканчивается в ${endHH}:${endMM}, рабочий день до ${WORK_END}:00`;
        errorEl.classList.remove('hidden');
        previewEl.classList.add('hidden');
        submitBtn.disabled = true;
    } else {
        previewEl.textContent = `Окончание: ${endHH}:${endMM}`;
        previewEl.classList.remove('hidden');
        errorEl.classList.add('hidden');
        submitBtn.disabled = false;
    }
}

// ── View switch ────────────────────────────────────────────────────────────
function switchView(view) {
    currentView = view;

    const wrapper = document.getElementById('cal-wrapper');
    const scheduleEl = document.getElementById('cal-schedule');
    const btnCal = document.getElementById('btn-view-calendar');
    const btnSch = document.getElementById('btn-view-schedule');

    if (view === 'calendar') {
        wrapper.classList.remove('hidden');
        scheduleEl.classList.add('hidden');
        btnCal.classList.add('cal-view-btn-active');
        btnSch.classList.remove('cal-view-btn-active');
        renderGrid();
    } else {
        wrapper.classList.add('hidden');
        scheduleEl.classList.remove('hidden');
        btnCal.classList.remove('cal-view-btn-active');
        btnSch.classList.add('cal-view-btn-active');
        renderSchedule();
    }
}

function renderSchedule() {
    const scheduleEl = document.getElementById('cal-schedule');
    const days = getWeekDays(currentWeekStart);
    const workDays = days.filter(d => WORKING_DAYS.includes(d.getDay()));

    const list = document.createElement('div');
    list.className = 'cal-schedule-list';

    workDays.forEach(day => {
        const dayAppts = appointments
            .filter(a => isSameDay(new Date(a.start_time), day))
            .sort((a, b) => new Date(a.start_time) - new Date(b.start_time));

        const dayEl = document.createElement('div');
        dayEl.className = 'cal-schedule-day';

        const header = document.createElement('div');
        header.className = 'cal-schedule-day-header';
        header.textContent = `${DAY_FULL_NAMES[day.getDay()]}, ${day.getDate()} ${MONTHS[day.getMonth()]} ${day.getFullYear()}`;
        dayEl.appendChild(header);

        if (dayAppts.length === 0) {
            const empty = document.createElement('div');
            empty.className = 'cal-schedule-empty';
            empty.textContent = 'Нет записей';
            dayEl.appendChild(empty);
        } else {
            dayAppts.forEach(appt => {
                const row = document.createElement('div');
                row.className = 'cal-schedule-row' + (appt.appointment_status === 'completed' ? ' cal-schedule-row--completed' : '');
                const statusLabel = STATUS_LABELS[appt.appointment_status] || appt.appointment_status;
                row.innerHTML = `
                    <span class="cal-schedule-time">${formatTime(appt.start_time)}</span>
                    <span class="cal-schedule-name">${escHtml(appt.client_name)}</span>
                    <span class="cal-schedule-service">${escHtml(appt.service || '—')}</span>
                    <span class="cal-badge cal-badge-${appt.appointment_status}">${escHtml(statusLabel)}</span>
                `;
                row.addEventListener('click', () => openEditModal(appt));
                dayEl.appendChild(row);
            });
        }

        list.appendChild(dayEl);
    });

    scheduleEl.innerHTML = '';
    scheduleEl.appendChild(list);
}

// ── Auth ───────────────────────────────────────────────────────────────────
async function logout() {
    await fetch('/api/logout', { method: 'POST' });
    window.location.href = '/';
}

// ── Utilities ──────────────────────────────────────────────────────────────
function getMonday(date) {
    const d = new Date(date);
    const day = d.getDay();
    const diff = day === 0 ? -6 : 1 - day;
    d.setDate(d.getDate() + diff);
    d.setHours(0, 0, 0, 0);
    return d;
}

function getWeekDays(monday) {
    return Array.from({ length: 7 }, (_, i) => {
        const d = new Date(monday);
        d.setDate(d.getDate() + i);
        return d;
    });
}

function isSameDay(a, b) {
    return a.getFullYear() === b.getFullYear()
        && a.getMonth() === b.getMonth()
        && a.getDate() === b.getDate();
}

function formatDateParam(date) {
    const y = date.getFullYear();
    const m = String(date.getMonth() + 1).padStart(2, '0');
    const d = String(date.getDate()).padStart(2, '0');
    return `${y}-${m}-${d}`;
}

function toDatetimeLocal(date) {
    const y = date.getFullYear();
    const mo = String(date.getMonth() + 1).padStart(2, '0');
    const d = String(date.getDate()).padStart(2, '0');
    const h = String(date.getHours()).padStart(2, '0');
    const mi = String(date.getMinutes()).padStart(2, '0');
    return `${y}-${mo}-${d}T${h}:${mi}`;
}

function formatTime(isoString) {
    const d = new Date(isoString);
    const h = String(d.getHours()).padStart(2, '0');
    const m = String(d.getMinutes()).padStart(2, '0');
    return `${h}:${m}`;
}

function updateWeekLabel() {
    const days = getWeekDays(currentWeekStart);
    const first = days[0];
    const last = days[6];
    const label = document.getElementById('cal-week-label');

    if (first.getMonth() === last.getMonth()) {
        label.textContent =
            `${first.getDate()} – ${last.getDate()} ${MONTHS[first.getMonth()]} ${first.getFullYear()}`;
    } else {
        label.textContent =
            `${first.getDate()} ${MONTHS[first.getMonth()]} – ${last.getDate()} ${MONTHS[last.getMonth()]} ${last.getFullYear()}`;
    }
}

function escHtml(str) {
    return String(str)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;');
}
