/**
 * TS Laser CRM - Frontend Application
 */

// ============== State ==============
let clients = [];
let partners = [];
let currentClientId = null;
let currentPartnerId = null;
let clientsSortBy = 'name';
let clientsSortOrder = 'asc';
let partnersSortBy = 'name';
let partnersSortOrder = 'asc';

// ============== API Helpers ==============
async function apiRequest(url, options = {}) {
    try {
        const response = await fetch(url, {
            headers: {
                'Content-Type': 'application/json',
                ...options.headers
            },
            ...options
        });
        
        if (response.status === 401) {
            window.location.href = '/login';
            return null;
        }
        
        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.detail || 'Ошибка запроса');
        }
        
        return await response.json();
    } catch (error) {
        console.error('API Error:', error);
        throw error;
    }
}

// ============== Auth ==============
async function logout() {
    try {
        await apiRequest('/api/logout', { method: 'POST' });
        window.location.href = '/login';
    } catch (error) {
        alert('Ошибка при выходе');
    }
}

// ============== Tab Switching ==============
function switchTab(tab) {
    // Update tab buttons
    document.querySelectorAll('.tab').forEach(t => {
        t.classList.toggle('active', t.dataset.tab === tab);
    });
    
    // Show/hide content
    document.getElementById('clients-tab').classList.toggle('hidden', tab !== 'clients');
    document.getElementById('partners-tab').classList.toggle('hidden', tab !== 'partners');
    
    // Load data
    if (tab === 'clients') {
        loadClients();
    } else {
        loadPartners();
    }
}

// ============== Formatting Helpers ==============
function formatPhone(phone) {
    if (!phone || phone.length !== 10) return phone || '—';
    return `+7 (${phone.slice(0, 3)}) ${phone.slice(3, 6)}-${phone.slice(6, 8)}-${phone.slice(8, 10)}`;
}

let isFormattingPhone = false;

function formatPhoneInput(input) {
    // Prevent recursion when we set the value
    if (isFormattingPhone) return;
    isFormattingPhone = true;
    
    // Get current raw digits stored
    const prevDigits = input.dataset.rawPhone || '';
    
    // Get all digits from current input
    let allDigits = input.value.replace(/\D/g, '');
    
    // The problem: "+7 (928)" contains "7928" as digits
    // We need to detect if the "7" is from our formatting or user input
    
    let newDigits;
    
    // If starts with 7 and we had previous digits, the 7 is from "+7"
    if (allDigits.startsWith('7') && prevDigits.length > 0) {
        // Remove the leading 7 (it's from +7 formatting)
        allDigits = allDigits.slice(1);
    }
    
    // If user pasted a full number with 8 or 7 prefix
    if (allDigits.length > 10 && (allDigits.startsWith('7') || allDigits.startsWith('8'))) {
        allDigits = allDigits.slice(1);
    }
    
    // Limit to 10 digits
    newDigits = allDigits.slice(0, 10);
    
    // Format the phone number dynamically
    let formatted = '';
    if (newDigits.length > 0) {
        formatted = '+7 (' + newDigits.slice(0, 3);
    }
    if (newDigits.length > 3) {
        formatted += ') ' + newDigits.slice(3, 6);
    }
    if (newDigits.length > 6) {
        formatted += '-' + newDigits.slice(6, 8);
    }
    if (newDigits.length > 8) {
        formatted += '-' + newDigits.slice(8, 10);
    }
    
    // Store raw digits in data attribute for form submission
    input.dataset.rawPhone = newDigits;
    input.value = formatted;
    
    // Place cursor at the end
    input.setSelectionRange(formatted.length, formatted.length);
    
    isFormattingPhone = false;
}

function getRawPhone(input) {
    return input.dataset.rawPhone || input.value.replace(/\D/g, '');
}

function formatDate(dateStr) {
    if (!dateStr) return '—';
    const date = new Date(dateStr);
    return date.toLocaleDateString('ru-RU');
}

function formatDateTime(dateStr) {
    if (!dateStr) return '—';
    const date = new Date(dateStr);
    return date.toLocaleDateString('ru-RU') + ' ' + date.toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' });
}

function getStatusTag(status) {
    const statusMap = {
        'active': { label: 'В процессе', class: 'tag-active' },
        'completed': { label: 'Завершено', class: 'tag-completed' },
        'stopped': { label: 'Перестал ходить', class: 'tag-stopped' },
        'lost': { label: 'Потерялся', class: 'tag-lost' }
    };
    const s = statusMap[status] || { label: status, class: '' };
    return `<span class="tag ${s.class}">${s.label}</span>`;
}

function calculateAge(birthDate) {
    if (!birthDate) return null;
    const today = new Date();
    const birth = new Date(birthDate);
    let age = today.getFullYear() - birth.getFullYear();
    const monthDiff = today.getMonth() - birth.getMonth();
    if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < birth.getDate())) {
        age--;
    }
    return age;
}

// ============== Form Show/Hide ==============
function showClientForm() {
    resetClientForm();
    document.getElementById('client-form-title').textContent = 'Добавить клиента';
    document.getElementById('client-form-modal').classList.remove('hidden');
    document.body.style.overflow = 'hidden';
    setTimeout(() => document.getElementById('client-name').focus(), 100);
}

function hideClientForm(e) {
    if (e && e.target !== e.currentTarget) return;
    document.getElementById('client-form-modal').classList.add('hidden');
    document.body.style.overflow = '';
    resetClientForm();
}

function showPartnerForm() {
    resetPartnerForm();
    document.getElementById('partner-form-title').textContent = 'Добавить партнёра';
    document.getElementById('partner-form-modal').classList.remove('hidden');
    document.body.style.overflow = 'hidden';
    setTimeout(() => document.getElementById('partner-name').focus(), 100);
}

function hidePartnerForm(e) {
    if (e && e.target !== e.currentTarget) return;
    document.getElementById('partner-form-modal').classList.add('hidden');
    document.body.style.overflow = '';
    resetPartnerForm();
}

// ============== Partners ==============
// Store all unique partner types
let allPartnerTypes = [];

async function loadPartners() {
    try {
        const search = document.getElementById('partners-search').value;
        const typeFilter = document.getElementById('partners-type-filter').value;
        const params = new URLSearchParams({
            sort_by: partnersSortBy,
            sort_order: partnersSortOrder
        });
        if (search) params.append('search', search);
        if (typeFilter) params.append('type_filter', typeFilter);
        
        partners = await apiRequest(`/api/partners?${params}`);
        renderPartnersTable();
        updatePartnerSelect();
        
        // Update types list when not filtering by type
        if (!typeFilter) {
            updatePartnerTypeFilter();
        }
    } catch (error) {
        console.error('Error loading partners:', error);
    }
}

// Update partner type filter dropdown with unique types
function updatePartnerTypeFilter() {
    const filterSelect = document.getElementById('partners-type-filter');
    const currentValue = filterSelect.value;
    
    // Get unique types from all partners
    allPartnerTypes = [...new Set(partners.map(p => p.type).filter(t => t))].sort();
    
    filterSelect.innerHTML = '<option value="">Все типы</option>' +
        allPartnerTypes.map(t => `<option value="${escapeHtml(t)}">${escapeHtml(t)}</option>`).join('');
    
    // Restore selected value
    if (currentValue) {
        filterSelect.value = currentValue;
    }
}

function renderPartnersTable() {
    const tbody = document.getElementById('partners-table-body');
    const emptyState = document.getElementById('partners-empty');
    const tableContainer = document.querySelector('#partners-tab .table-container');
    
    if (partners.length === 0) {
        tbody.innerHTML = '';
        emptyState.classList.remove('hidden');
        tableContainer.classList.add('hidden');
        return;
    }
    
    emptyState.classList.add('hidden');
    tableContainer.classList.remove('hidden');
    tbody.innerHTML = partners.map(p => `
        <tr onclick="openPartnerModal(${p.id})" style="cursor: pointer;">
            <td><strong>${escapeHtml(p.name)}</strong></td>
            <td>${escapeHtml(p.contacts) || '—'}</td>
            <td>${escapeHtml(p.type) || '—'}</td>
            <td>${escapeHtml(p.terms) || '—'}</td>
            <td class="text-muted text-sm">${formatDate(p.created_at)}</td>
            <td onclick="event.stopPropagation()">
                <div class="flex gap-2">
                    <button class="btn btn-secondary btn-sm" onclick="editPartner(${p.id})">✏️</button>
                    <button class="btn btn-danger btn-sm" onclick="deletePartner(${p.id})">🗑️</button>
                </div>
            </td>
        </tr>
    `).join('');
}

function updatePartnerSelect() {
    // Update partner select in client form
    const select = document.getElementById('client-referral-partner');
    const currentValue = select.value;
    select.innerHTML = '<option value="">— Не из партнёрской базы —</option>' +
        partners.map(p => `<option value="${p.id}">${escapeHtml(p.name)}</option>`).join('');
    select.value = currentValue;
    
    // Update partner filter dropdown
    const filterSelect = document.getElementById('clients-partner-filter');
    const filterCurrentValue = filterSelect.value;
    filterSelect.innerHTML = '<option value="">Все партнёры</option>' +
        partners.map(p => `<option value="${p.id}">${escapeHtml(p.name)}</option>`).join('');
    filterSelect.value = filterCurrentValue;
}

async function savePartner(e) {
    e.preventDefault();
    
    const id = document.getElementById('partner-id').value;
    const data = {
        name: document.getElementById('partner-name').value.trim(),
        contacts: document.getElementById('partner-contacts').value.trim() || null,
        type: document.getElementById('partner-type').value.trim() || null,
        terms: document.getElementById('partner-terms').value.trim() || null,
        comment: document.getElementById('partner-comment').value.trim() || null
    };
    
    try {
        if (id) {
            await apiRequest(`/api/partners/${id}`, {
                method: 'PUT',
                body: JSON.stringify(data)
            });
        } else {
            await apiRequest('/api/partners', {
                method: 'POST',
                body: JSON.stringify(data)
            });
        }
        
        hidePartnerForm();
        loadPartners();
    } catch (error) {
        alert('Ошибка сохранения: ' + error.message);
    }
}

async function editPartner(id) {
    const partner = partners.find(p => p.id === id);
    if (!partner) return;
    
    document.getElementById('partner-id').value = partner.id;
    document.getElementById('partner-name').value = partner.name || '';
    document.getElementById('partner-contacts').value = partner.contacts || '';
    document.getElementById('partner-type').value = partner.type || '';
    document.getElementById('partner-terms').value = partner.terms || '';
    document.getElementById('partner-comment').value = partner.comment || '';
    
    document.getElementById('partner-form-title').textContent = 'Редактировать партнёра';
    document.getElementById('partner-form-modal').classList.remove('hidden');
    document.body.style.overflow = 'hidden';
}

async function deletePartner(id) {
    if (!confirm('Удалить этого партнёра?')) return;
    
    try {
        await apiRequest(`/api/partners/${id}`, { method: 'DELETE' });
        loadPartners();
        closePartnerModal();
    } catch (error) {
        alert('Ошибка удаления: ' + error.message);
    }
}

function resetPartnerForm() {
    document.getElementById('partner-form').reset();
    document.getElementById('partner-id').value = '';
}

function cancelPartnerEdit() {
    hidePartnerForm();
}

function openPartnerModal(id) {
    const partner = partners.find(p => p.id === id);
    if (!partner) return;
    
    currentPartnerId = id;
    
    document.getElementById('modal-partner-name').textContent = partner.name;
    document.getElementById('modal-partner-body').innerHTML = `
        <div class="client-card-info">
            <div class="client-card-field">
                <span class="client-card-label">Контакты:</span>
                <span>${escapeHtml(partner.contacts) || '—'}</span>
            </div>
            <div class="client-card-field">
                <span class="client-card-label">Тип:</span>
                <span>${escapeHtml(partner.type) || '—'}</span>
            </div>
            <div class="client-card-field">
                <span class="client-card-label">Условия:</span>
                <span>${escapeHtml(partner.terms) || '—'}</span>
            </div>
            <div class="client-card-field">
                <span class="client-card-label">Комментарий:</span>
                <span>${escapeHtml(partner.comment) || '—'}</span>
            </div>
            <div class="client-card-field text-muted text-sm mt-4">
                <span class="client-card-label">Создан:</span>
                <span>${formatDateTime(partner.created_at)}</span>
            </div>
            <div class="client-card-field text-muted text-sm">
                <span class="client-card-label">Изменён:</span>
                <span>${formatDateTime(partner.updated_at)}</span>
            </div>
        </div>
    `;
    
    document.getElementById('partner-modal').classList.remove('hidden');
    document.body.style.overflow = 'hidden';
}

function closePartnerModal(e) {
    if (e && e.target !== e.currentTarget) return;
    document.getElementById('partner-modal').classList.add('hidden');
    document.body.style.overflow = '';
    currentPartnerId = null;
}

function editPartnerFromModal() {
    const id = currentPartnerId;
    if (id) {
        closePartnerModal();
        editPartner(id);
    }
}

function deletePartnerFromModal() {
    if (currentPartnerId) {
        deletePartner(currentPartnerId);
    }
}

// ============== Clients ==============
async function loadClients() {
    try {
        const search = document.getElementById('clients-search').value;
        const statusFilter = document.getElementById('clients-status-filter').value;
        const partnerFilter = document.getElementById('clients-partner-filter').value;
        
        const params = new URLSearchParams({
            sort_by: clientsSortBy,
            sort_order: clientsSortOrder
        });
        if (search) params.append('search', search);
        if (statusFilter) params.append('status_filter', statusFilter);
        if (partnerFilter) params.append('partner_filter', partnerFilter);
        
        clients = await apiRequest(`/api/clients?${params}`);
        renderClientsTable();
    } catch (error) {
        console.error('Error loading clients:', error);
    }
}

function renderClientsTable() {
    const tbody = document.getElementById('clients-table-body');
    const emptyState = document.getElementById('clients-empty');
    const tableContainer = document.querySelector('#clients-tab .table-container');
    
    if (clients.length === 0) {
        tbody.innerHTML = '';
        emptyState.classList.remove('hidden');
        tableContainer.classList.add('hidden');
        return;
    }
    
    emptyState.classList.add('hidden');
    tableContainer.classList.remove('hidden');
    tbody.innerHTML = clients.map(c => `
        <tr onclick="openClientModal(${c.id})" style="cursor: pointer;">
            <td><strong>${escapeHtml(c.name)}</strong></td>
            <td>${formatPhone(c.phone)}</td>
            <td>${escapeHtml(c.address) || '—'}</td>
            <td>${getStatusTag(c.status)}</td>
            <td class="text-muted text-sm">${formatDate(c.created_at)}</td>
            <td onclick="event.stopPropagation()">
                <div class="flex gap-2">
                    <a href="/clients/${c.id}/sessions" class="btn btn-primary btn-sm" title="Сеансы">💉</a>
                    <button class="btn btn-secondary btn-sm" onclick="editClient(${c.id})">✏️</button>
                    <button class="btn btn-danger btn-sm" onclick="deleteClient(${c.id})">🗑️</button>
                </div>
            </td>
        </tr>
    `).join('');
}

async function loadClientDetails(id) {
    try {
        return await apiRequest(`/api/clients/${id}`);
    } catch (error) {
        console.error('Error loading client details:', error);
        return null;
    }
}

async function saveClient(e) {
    e.preventDefault();
    
    const id = document.getElementById('client-id').value;
    const birthDate = document.getElementById('client-birth-date').value;
    const ageInput = document.getElementById('client-age').value;
    const genderInput = document.querySelector('input[name="client-gender"]:checked');
    const referralPartnerId = document.getElementById('client-referral-partner').value;
    
    const phoneInput = document.getElementById('client-phone');
    const data = {
        name: document.getElementById('client-name').value.trim(),
        phone: getRawPhone(phoneInput) || null,
        birth_date: birthDate || null,
        age: ageInput ? parseInt(ageInput) : (birthDate ? calculateAge(birthDate) : null),
        gender: genderInput ? genderInput.value : null,
        address: document.getElementById('client-address').value.trim() || null,
        referral_partner_id: referralPartnerId ? parseInt(referralPartnerId) : null,
        referral_custom: document.getElementById('client-referral-custom').value.trim() || null,
        status: document.getElementById('client-status').value,
        stopped_reason: document.getElementById('client-stopped-reason').value.trim() || null
    };
    
    try {
        if (id) {
            await apiRequest(`/api/clients/${id}`, {
                method: 'PUT',
                body: JSON.stringify(data)
            });
        } else {
            await apiRequest('/api/clients', {
                method: 'POST',
                body: JSON.stringify(data)
            });
        }
        
        hideClientForm();
        loadClients();
    } catch (error) {
        alert('Ошибка сохранения: ' + error.message);
    }
}

async function editClient(id) {
    const client = await loadClientDetails(id);
    if (!client) return;
    
    document.getElementById('client-id').value = client.id;
    document.getElementById('client-name').value = client.name || '';
    
    // Set phone with formatting
    const phoneInput = document.getElementById('client-phone');
    if (client.phone) {
        phoneInput.dataset.rawPhone = client.phone;
        phoneInput.value = formatPhone(client.phone);
    } else {
        phoneInput.value = '';
        phoneInput.dataset.rawPhone = '';
    }
    document.getElementById('client-birth-date').value = client.birth_date || '';
    document.getElementById('client-age').value = client.age || '';
    document.getElementById('client-address').value = client.address || '';
    document.getElementById('client-referral-partner').value = client.referral_partner_id || '';
    document.getElementById('client-referral-custom').value = client.referral_custom || '';
    document.getElementById('client-status').value = client.status;
    document.getElementById('client-stopped-reason').value = client.stopped_reason || '';
    
    // Set gender radio
    const genderRadio = document.querySelector(`input[name="client-gender"][value="${client.gender}"]`);
    if (genderRadio) genderRadio.checked = true;
    
    // Show/hide stopped reason
    toggleStoppedReason();
    
    document.getElementById('client-form-title').textContent = 'Редактировать клиента';
    document.getElementById('client-form-modal').classList.remove('hidden');
    document.body.style.overflow = 'hidden';
}

async function deleteClient(id) {
    if (!confirm('Удалить этого клиента?')) return;
    
    try {
        await apiRequest(`/api/clients/${id}`, { method: 'DELETE' });
        loadClients();
        closeClientModal();
    } catch (error) {
        alert('Ошибка удаления: ' + error.message);
    }
}

function resetClientForm() {
    document.getElementById('client-form').reset();
    document.getElementById('client-id').value = '';
    document.getElementById('client-phone').dataset.rawPhone = '';
    document.getElementById('stopped-reason-group').classList.add('hidden');
}

function cancelClientEdit() {
    hideClientForm();
}

async function openClientModal(id) {
    const client = await loadClientDetails(id);
    if (!client) return;
    
    currentClientId = id;
    
    document.getElementById('modal-client-name').textContent = client.name;
    
    // Get partner name
    let partnerName = '—';
    if (client.referral_partner_id) {
        const partner = partners.find(p => p.id === client.referral_partner_id);
        partnerName = partner ? partner.name : `ID: ${client.referral_partner_id}`;
    }
    
    document.getElementById('modal-client-body').innerHTML = `
        <div class="mb-4">${getStatusTag(client.status)}</div>
        <div class="client-card-info">
            <div class="client-card-field">
                <span class="client-card-label">Телефон:</span>
                <span>${formatPhone(client.phone)}</span>
            </div>
            <div class="client-card-field">
                <span class="client-card-label">Дата рождения:</span>
                <span>${formatDate(client.birth_date)}</span>
            </div>
            <div class="client-card-field">
                <span class="client-card-label">Возраст:</span>
                <span>${client.age || '—'}</span>
            </div>
            <div class="client-card-field">
                <span class="client-card-label">Пол:</span>
                <span>${client.gender === 'М' ? 'Мужской' : client.gender === 'Ж' ? 'Женский' : '—'}</span>
            </div>
            <div class="client-card-field">
                <span class="client-card-label">Адрес:</span>
                <span>${escapeHtml(client.address) || '—'}</span>
            </div>
            <div class="client-card-field">
                <span class="client-card-label">Откуда узнали:</span>
                <span>${client.referral_partner_id ? partnerName : ''} ${escapeHtml(client.referral_custom) || (client.referral_partner_id ? '' : '—')}</span>
            </div>
            ${client.status === 'stopped' ? `
            <div class="client-card-field">
                <span class="client-card-label">Причина ухода:</span>
                <span>${escapeHtml(client.stopped_reason) || '—'}</span>
            </div>
            ` : ''}
            <div class="client-card-field text-muted text-sm mt-4">
                <span class="client-card-label">Создан:</span>
                <span>${formatDateTime(client.created_at)}</span>
            </div>
            <div class="client-card-field text-muted text-sm">
                <span class="client-card-label">Изменён:</span>
                <span>${formatDateTime(client.updated_at)}</span>
            </div>
        </div>
    `;
    
    // Set sessions button link
    document.getElementById('modal-client-sessions-btn').href = `/clients/${id}/sessions`;
    
    document.getElementById('client-modal').classList.remove('hidden');
    document.body.style.overflow = 'hidden';
}

function closeClientModal(e) {
    if (e && e.target !== e.currentTarget) return;
    document.getElementById('client-modal').classList.add('hidden');
    document.body.style.overflow = '';
    currentClientId = null;
}

function editClientFromModal() {
    const id = currentClientId;
    if (id) {
        closeClientModal();
        editClient(id);
    }
}

function deleteClientFromModal() {
    if (currentClientId) {
        deleteClient(currentClientId);
    }
}

function toggleStoppedReason() {
    const status = document.getElementById('client-status').value;
    const reasonGroup = document.getElementById('stopped-reason-group');
    reasonGroup.classList.toggle('hidden', status !== 'stopped');
}

// ============== Sorting ==============
function handleTableSort(tableId, sortField) {
    if (tableId === 'clients') {
        if (clientsSortBy === sortField) {
            clientsSortOrder = clientsSortOrder === 'asc' ? 'desc' : 'asc';
        } else {
            clientsSortBy = sortField;
            clientsSortOrder = 'asc';
        }
        loadClients();
    } else if (tableId === 'partners') {
        if (partnersSortBy === sortField) {
            partnersSortOrder = partnersSortOrder === 'asc' ? 'desc' : 'asc';
        } else {
            partnersSortBy = sortField;
            partnersSortOrder = 'asc';
        }
        loadPartners();
    }
}

// ============== Utilities ==============
function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

// ============== Event Listeners ==============
document.addEventListener('DOMContentLoaded', () => {
    // Forms
    document.getElementById('client-form').addEventListener('submit', saveClient);
    document.getElementById('partner-form').addEventListener('submit', savePartner);
    
    // Status change handler
    document.getElementById('client-status').addEventListener('change', toggleStoppedReason);
    
    // Phone formatting on input
    document.getElementById('client-phone').addEventListener('input', (e) => {
        formatPhoneInput(e.target);
    });
    
    // Birth date -> age calculation
    document.getElementById('client-birth-date').addEventListener('change', (e) => {
        const age = calculateAge(e.target.value);
        if (age !== null) {
            document.getElementById('client-age').value = age;
        }
    });
    
    // Search inputs with debounce
    document.getElementById('clients-search').addEventListener('input', debounce(loadClients, 300));
    document.getElementById('partners-search').addEventListener('input', debounce(loadPartners, 300));
    
    // Status filter
    document.getElementById('clients-status-filter').addEventListener('change', loadClients);
    
    // Partner type filter
    document.getElementById('partners-type-filter').addEventListener('change', loadPartners);
    
    // Partner filter
    document.getElementById('clients-partner-filter').addEventListener('change', loadClients);
    
    // Table sorting
    document.querySelectorAll('#clients-tab .table th.sortable').forEach(th => {
        th.addEventListener('click', () => handleTableSort('clients', th.dataset.sort));
    });
    document.querySelectorAll('#partners-tab .table th.sortable').forEach(th => {
        th.addEventListener('click', () => handleTableSort('partners', th.dataset.sort));
    });
    
    // Keyboard shortcuts
    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape') {
            // Close modals in order
            if (!document.getElementById('client-form-modal').classList.contains('hidden')) {
                hideClientForm();
            } else if (!document.getElementById('partner-form-modal').classList.contains('hidden')) {
                hidePartnerForm();
            } else if (!document.getElementById('client-modal').classList.contains('hidden')) {
                closeClientModal();
            } else if (!document.getElementById('partner-modal').classList.contains('hidden')) {
                closePartnerModal();
            }
        }
    });
    
    // Initial load
    loadPartners().then(() => {
        loadClients().then(() => {
            // Check if we need to open a client modal (coming back from sessions page)
            const openClientModalId = sessionStorage.getItem('openClientModal');
            if (openClientModalId) {
                sessionStorage.removeItem('openClientModal');
                openClientModal(parseInt(openClientModalId));
            }
        });
    });
    
    // Close export menus when clicking outside
    document.addEventListener('click', function(e) {
        if (!e.target.closest('.export-dropdown')) {
            document.querySelectorAll('.export-menu').forEach(menu => {
                menu.classList.add('hidden');
            });
        }
    });
});

// ============== Export Functions ==============

function toggleExportMenu(entity) {
    const menu = document.getElementById(`${entity}-export-menu`);
    // Close all other menus
    document.querySelectorAll('.export-menu').forEach(m => {
        if (m !== menu) m.classList.add('hidden');
    });
    menu.classList.toggle('hidden');
}

function exportData(entity, format) {
    let url = '';
    if (entity === 'clients') {
        url = `/api/export/clients?format=${format}`;
    } else if (entity === 'partners') {
        url = `/api/export/partners?format=${format}`;
    }
    
    if (url) {
        window.location.href = url;
    }
    
    // Close the menu
    document.querySelectorAll('.export-menu').forEach(menu => {
        menu.classList.add('hidden');
    });
}
