let bookings = [];
let currentBooking = null;
let isSubmitting = false;

async function apiRequest(url, options = {}) {
    const response = await fetch(url, {
        headers: {
            "Content-Type": "application/json",
            ...(options.headers || {})
        },
        ...options
    });

    if (response.status === 401) {
        window.location.href = "/login";
        return null;
    }

    if (!response.ok) {
        let message = "Ошибка запроса";
        try {
            const payload = await response.json();
            message = payload.detail || message;
        } catch (e) {
            // ignored
        }
        throw new Error(message);
    }

    if (response.status === 204) {
        return null;
    }

    return response.json();
}

async function logout() {
    try {
        await apiRequest("/api/logout", { method: "POST" });
        window.location.href = "/login";
    } catch (e) {
        alert("Не удалось выйти");
    }
}

function escapeHtml(value) {
    if (!value) return "";
    const div = document.createElement("div");
    div.textContent = value;
    return div.innerHTML;
}

function formatDateTime(value) {
    if (!value) return "—";
    const date = new Date(value);
    return date.toLocaleDateString("ru-RU") + " " + date.toLocaleTimeString("ru-RU", { hour: "2-digit", minute: "2-digit" });
}

function formatPhone(value) {
    if (!value || value.length !== 10) return value || "—";
    return `+7 (${value.slice(0, 3)}) ${value.slice(3, 6)}-${value.slice(6, 8)}-${value.slice(8, 10)}`;
}

function getStatusTag(status) {
    const map = {
        pending: { label: "Новая", className: "tag-active" },
        approved: { label: "Одобрена", className: "tag-completed" },
        rejected: { label: "Отклонена", className: "tag-stopped" },
        completed: { label: "Завершено", className: "tag-lost" }
    };

    const cfg = map[status] || { label: status || "unknown", className: "tag-lost" };
    return `<span class="tag ${cfg.className}">${cfg.label}</span>`;
}

async function loadBookings() {
    try {
        const status = document.getElementById("bookings-status-filter").value;
        bookings = await apiRequest(`/api/bookings?status=${encodeURIComponent(status)}`) || [];
        renderBookings();
    } catch (e) {
        console.error(e);
        alert(e.message || "Не удалось загрузить заявки");
    }
}

function renderBookings() {
    const tbody = document.getElementById("bookings-table-body");
    const tableContainer = document.getElementById("bookings-table-container");
    const emptyState = document.getElementById("bookings-empty");

    if (!bookings.length) {
        tbody.innerHTML = "";
        tableContainer.classList.add("hidden");
        emptyState.classList.remove("hidden");
        return;
    }

    emptyState.classList.add("hidden");
    tableContainer.classList.remove("hidden");

    tbody.innerHTML = bookings.map((item) => `
        <tr onclick="openBooking(${item.id})" style="cursor: pointer;">
            <td>${item.id}</td>
            <td>${formatDateTime(item.created_at)}</td>
            <td><strong>${escapeHtml(item.full_name)}</strong></td>
            <td>${formatPhone(item.phone)}</td>
            <td>${escapeHtml(item.tattoo_type) || "—"}</td>
            <td>${getStatusTag(item.status)}</td>
        </tr>
    `).join("");
}

async function openBooking(id) {
    try {
        const booking = await apiRequest(`/api/bookings/${id}`);
        if (!booking) return;

        currentBooking = booking;
        document.getElementById("booking-modal-title").textContent = `Заявка #${booking.id}`;

        document.getElementById("booking-modal-body").innerHTML = `
            <div class="mb-4">${getStatusTag(booking.status)}</div>
            <div class="detail-row"><span class="detail-label">ФИО</span><span class="detail-value">${escapeHtml(booking.full_name)}</span></div>
            <div class="detail-row"><span class="detail-label">Пол</span><span class="detail-value">${escapeHtml(booking.gender) || "—"}</span></div>
            <div class="detail-row"><span class="detail-label">Телефон</span><span class="detail-value">${formatPhone(booking.phone)}</span></div>
            <div class="detail-row"><span class="detail-label">Дата рождения</span><span class="detail-value">${booking.birth_date || "—"}</span></div>
            <div class="detail-row"><span class="detail-label">Возраст</span><span class="detail-value">${booking.age !== null && booking.age !== undefined ? booking.age + ' лет' : '—'}</span></div>
            <div class="detail-row"><span class="detail-label">Адрес</span><span class="detail-value">${escapeHtml(booking.address) || "—"}</span></div>
            <div class="detail-row"><span class="detail-label">Источник</span><span class="detail-value">${escapeHtml(booking.referral_source) || "—"}</span></div>
            <div class="detail-row"><span class="detail-label">Тип тату</span><span class="detail-value">${escapeHtml(booking.tattoo_type) || "—"}</span></div>
            <div class="detail-row"><span class="detail-label">Возраст тату</span><span class="detail-value">${escapeHtml(booking.tattoo_age) || "—"}</span></div>
            <div class="detail-row"><span class="detail-label">Коррекции</span><span class="detail-value">${escapeHtml(booking.corrections_info) || "—"}</span></div>
            <div class="detail-row"><span class="detail-label">Удаление ранее</span><span class="detail-value">${escapeHtml(booking.previous_removal_info) || "—"}</span></div>
            <div class="detail-row"><span class="detail-label">Где удаляли</span><span class="detail-value">${escapeHtml(booking.previous_removal_where) || "—"}</span></div>
            <div class="detail-row"><span class="detail-label">Желаемый результат</span><span class="detail-value">${escapeHtml(booking.desired_result) || "—"}</span></div>
            <div class="detail-row"><span class="detail-label">Создана</span><span class="detail-value">${formatDateTime(booking.created_at)}</span></div>
            <div class="detail-row"><span class="detail-label">Проверена</span><span class="detail-value">${formatDateTime(booking.reviewed_at)}</span></div>
            <div class="detail-row"><span class="detail-label">Проверил</span><span class="detail-value">${escapeHtml(booking.reviewed_by) || "—"}</span></div>
            <div class="detail-row"><span class="detail-label">Причина отклонения</span><span class="detail-value">${escapeHtml(booking.rejection_reason) || "—"}</span></div>
        `;

        const isPending = booking.status === "pending";
        document.getElementById("booking-approve-btn").classList.toggle("hidden", !isPending);
        document.getElementById("booking-reject-btn").classList.toggle("hidden", !isPending);
        document.getElementById("booking-reject-wrap").classList.toggle("hidden", !isPending);
        document.getElementById("booking-reject-reason").value = "";

        document.getElementById("booking-modal").classList.remove("hidden");
        document.body.style.overflow = "hidden";
    } catch (e) {
        console.error(e);
        alert(e.message || "Не удалось открыть заявку");
    }
}

function closeBookingModal(event) {
    if (event && event.target !== event.currentTarget) return;
    document.getElementById("booking-modal").classList.add("hidden");
    document.body.style.overflow = "";
    currentBooking = null;
    isSubmitting = false;
}

async function approveBooking() {
    if (!currentBooking || isSubmitting) return;
    if (!confirm("Одобрить заявку?")) return;

    isSubmitting = true;
    try {
        await apiRequest(`/api/bookings/${currentBooking.id}/approve`, { method: "POST" });
        closeBookingModal();
        await loadBookings();
    } catch (e) {
        alert(e.message || "Не удалось одобрить заявку");
    } finally {
        isSubmitting = false;
    }
}

async function rejectBooking() {
    if (!currentBooking || isSubmitting) return;
    if (!confirm("Отклонить заявку?")) return;

    isSubmitting = true;
    try {
        const rejectionReason = document.getElementById("booking-reject-reason").value.trim();
        await apiRequest(`/api/bookings/${currentBooking.id}/reject`, {
            method: "POST",
            body: JSON.stringify({
                rejection_reason: rejectionReason || null
            })
        });
        closeBookingModal();
        await loadBookings();
    } catch (e) {
        alert(e.message || "Не удалось отклонить заявку");
    } finally {
        isSubmitting = false;
    }
}

document.addEventListener("DOMContentLoaded", () => {
    document.getElementById("bookings-status-filter").addEventListener("change", loadBookings);

    document.addEventListener("keydown", (e) => {
        if (e.key === "Escape") {
            closeBookingModal();
        }
    });

    loadBookings();
});
