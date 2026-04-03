// Sessions & Tattoos Page JavaScript

let sessions = [];
let tattoos = [];
let totalFlashes = 0;
let clientName = '';
let currentSessionId = null;
let currentTattooId = null;

// Sorting state (default: by date ascending)
let sessionsSortBy = 'session_date';
let sessionsSortOrder = 'asc';

// Store all unique tattoo names for filter (from sessions, for backwards compatibility)
let allTattooNames = [];

// ============== Tab Switching ==============
function switchSessionsTab(tab) {
    document.querySelectorAll('.tab').forEach(t => {
        t.classList.toggle('active', t.dataset.tab === tab);
    });
    document.querySelectorAll('.tab-content').forEach(c => {
        c.classList.toggle('hidden', c.id !== `${tab}-tab`);
    });
}

// ============== Utility Functions ==============

// Format date for display
function formatDate(dateStr) {
    if (!dateStr) return '—';
    const date = new Date(dateStr);
    return date.toLocaleDateString('ru-RU', { 
        day: '2-digit', 
        month: '2-digit', 
        year: 'numeric' 
    });
}

// Format date for input field
function formatDateForInput(dateStr) {
    if (!dateStr) return '';
    if (/^\d{4}-\d{2}-\d{2}$/.test(dateStr)) {
        return dateStr;
    }
    const date = new Date(dateStr);
    return date.toISOString().split('T')[0];
}

// Helper function to escape HTML
function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// Format tattoo name with section (e.g., "Рукав" or "Рукав (верх)")
function formatTattooName(session) {
    const name = session.tattoo_name || getTattooNameById(session.tattoo_id);
    if (!name) return '—';
    if (session.sub_session) {
        return `${escapeHtml(name)} <span class="text-muted">(${escapeHtml(session.sub_session)})</span>`;
    }
    return escapeHtml(name);
}

// Get tattoo name by ID
function getTattooNameById(tattooId) {
    if (!tattooId) return null;
    const tattoo = tattoos.find(t => t.id === tattooId);
    return tattoo ? tattoo.name : null;
}

// ============== Tattoos Management ==============

// Load tattoos data
async function loadTattoos() {
    try {
        const response = await fetch(`/api/clients/${CLIENT_ID}/tattoos`);
        
        if (response.status === 401) {
            window.location.href = '/login';
            return;
        }
        
        if (!response.ok) {
            throw new Error('Failed to load tattoos');
        }
        
        const data = await response.json();
        tattoos = data.tattoos;
        clientName = data.client_name;
        
        // Update client name display
        document.getElementById('client-name').textContent = clientName;
        
        renderTattoosTable();
        updateZoneSelect();
        updateTattooSelect();
    } catch (error) {
        console.error('Error loading tattoos:', error);
    }
}

// Render tattoos table
function renderTattoosTable() {
    const tbody = document.getElementById('tattoos-table-body');
    const emptyState = document.getElementById('tattoos-empty');
    const tableContainer = document.getElementById('tattoos-table-container');
    
    if (tattoos.length === 0) {
        tableContainer.classList.add('hidden');
        emptyState.classList.remove('hidden');
        return;
    }
    
    tableContainer.classList.remove('hidden');
    emptyState.classList.add('hidden');
    
    tbody.innerHTML = tattoos.map(tattoo => `
        <tr onclick="viewTattoo(${tattoo.id})" style="cursor: pointer;">
            <td><strong>${escapeHtml(tattoo.name)}</strong></td>
            <td>${escapeHtml(tattoo.removal_zone) || '—'}</td>
            <td>${escapeHtml(tattoo.corrections_count) || '—'}</td>
            <td>${formatDate(tattoo.last_pigment_date)}</td>
            <td>${tattoo.no_laser_before ? 'Не удалял' : formatDate(tattoo.last_laser_date)}</td>
            <td>${escapeHtml(tattoo.desired_result) || '—'}</td>
            <td onclick="event.stopPropagation()">
                <div class="actions-cell">
                    <button class="btn-icon btn-icon-edit" onclick="editTattoo(${tattoo.id})" title="Редактировать">✏️</button>
                    <button class="btn-icon btn-icon-delete" onclick="deleteTattoo(${tattoo.id})" title="Удалить">🗑️</button>
                </div>
            </td>
        </tr>
    `).join('');
}

// Update zone select in session form
function updateZoneSelect() {
    const select = document.getElementById('session-zone-select');
    const currentValue = select.value;
    
    // Get unique zones from tattoos
    const zones = [...new Set(tattoos.map(t => t.removal_zone).filter(z => z))];
    zones.sort();
    
    select.innerHTML = '<option value="">— Все зоны —</option>' +
        zones.map(z => 
            `<option value="${escapeHtml(z)}">${escapeHtml(z)}</option>`
        ).join('');
    
    // Restore previous value if it still exists
    if (currentValue && zones.includes(currentValue)) {
        select.value = currentValue;
    }
}

// Update tattoo select in session form (optionally filtered by zone)
function updateTattooSelect(zoneFilter = null) {
    const select = document.getElementById('session-tattoo-select');
    const currentValue = select.value;
    
    // Filter tattoos by zone if specified
    let filteredTattoos = tattoos;
    if (zoneFilter) {
        filteredTattoos = tattoos.filter(t => t.removal_zone === zoneFilter);
    }
    
    select.innerHTML = '<option value="">— Выберите татуировку/татуаж —</option>' +
        filteredTattoos.map(t => 
            `<option value="${t.id}">${escapeHtml(t.name)}${t.removal_zone ? ` (${escapeHtml(t.removal_zone)})` : ''}</option>`
        ).join('');
    
    // Restore previous value if it still exists in filtered list
    if (currentValue) {
        const stillExists = filteredTattoos.some(t => t.id == currentValue);
        if (stillExists) {
            select.value = currentValue;
        }
    }
}

// Handle zone selection change
function handleZoneSelectChange() {
    const zoneSelect = document.getElementById('session-zone-select');
    const selectedZone = zoneSelect.value;
    
    // Update tattoo select with filtered tattoos
    updateTattooSelect(selectedZone || null);
    
    // Clear tattoo selection if it's no longer in the filtered list
    const tattooSelect = document.getElementById('session-tattoo-select');
    if (tattooSelect.value === '') {
        // Also clear session number
        document.getElementById('session-number').value = '';
    }
}

// Handle tattoo selection change - auto-fill zone
function handleTattooSelectChangeForZone() {
    const tattooSelect = document.getElementById('session-tattoo-select');
    const zoneSelect = document.getElementById('session-zone-select');
    const selectedTattooId = tattooSelect.value;
    
    if (selectedTattooId) {
        const tattoo = tattoos.find(t => t.id == selectedTattooId);
        if (tattoo && tattoo.removal_zone) {
            // Auto-fill zone from tattoo
            zoneSelect.value = tattoo.removal_zone;
        }
    }
}

// Show tattoo form (modal)
function showTattooForm(tattooId = null) {
    currentTattooId = tattooId;
    const modal = document.getElementById('tattoo-form-modal');
    const form = document.getElementById('tattoo-form');
    const title = document.getElementById('tattoo-form-title');
    
    form.reset();
    
    if (tattooId) {
        title.textContent = 'Редактировать татуировку/татуаж';
        const tattoo = tattoos.find(t => t.id === tattooId);
        if (tattoo) {
            document.getElementById('tattoo-id').value = tattoo.id;
            document.getElementById('tattoo-name').value = tattoo.name || '';
            document.getElementById('tattoo-zone').value = tattoo.removal_zone || '';
            document.getElementById('tattoo-corrections').value = tattoo.corrections_count || '';
            document.getElementById('tattoo-last-pigment').value = formatDateForInput(tattoo.last_pigment_date);
            document.getElementById('tattoo-last-laser').value = formatDateForInput(tattoo.last_laser_date);
            document.getElementById('tattoo-no-laser').checked = tattoo.no_laser_before;
            document.getElementById('tattoo-prev-place').value = tattoo.previous_removal_place || '';
            document.getElementById('tattoo-desired-result').value = tattoo.desired_result || '';
            
            // Toggle laser fields based on checkbox
            toggleTattooLaserFields();
        }
    } else {
        title.textContent = 'Добавить татуировку/татуаж';
        document.getElementById('tattoo-id').value = '';
    }
    
    modal.classList.remove('hidden');
}

// Hide tattoo form
function hideTattooForm(event) {
    if (event && event.target !== event.currentTarget) return;
    const modal = document.getElementById('tattoo-form-modal');
    modal.classList.add('hidden');
    currentTattooId = null;
}

// Toggle laser fields based on "no laser before" checkbox
function toggleTattooLaserFields() {
    const noLaser = document.getElementById('tattoo-no-laser').checked;
    document.getElementById('tattoo-last-laser').disabled = noLaser;
    document.getElementById('tattoo-prev-place').disabled = noLaser;
    if (noLaser) {
        document.getElementById('tattoo-last-laser').value = '';
        document.getElementById('tattoo-prev-place').value = '';
    }
}

// Save tattoo
async function saveTattoo(event) {
    event.preventDefault();
    
    const tattooId = document.getElementById('tattoo-id').value;
    const tattooData = {
        name: document.getElementById('tattoo-name').value.trim(),
        removal_zone: document.getElementById('tattoo-zone').value.trim() || null,
        corrections_count: document.getElementById('tattoo-corrections').value.trim() || null,
        last_pigment_date: document.getElementById('tattoo-last-pigment').value || null,
        last_laser_date: document.getElementById('tattoo-last-laser').value || null,
        no_laser_before: document.getElementById('tattoo-no-laser').checked,
        previous_removal_place: document.getElementById('tattoo-prev-place').value.trim() || null,
        desired_result: document.getElementById('tattoo-desired-result').value.trim() || null
    };
    
    if (!tattooData.name) {
        alert('Введите название татуировки/татуажа');
        return;
    }
    
    try {
        let response;
        if (tattooId) {
            response = await fetch(`/api/tattoos/${tattooId}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(tattooData)
            });
        } else {
            response = await fetch(`/api/clients/${CLIENT_ID}/tattoos`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(tattooData)
            });
        }
        
        if (!response.ok) {
            throw new Error('Failed to save tattoo');
        }
        
        hideTattooForm();
        await loadTattoos();
        await loadSessions(); // Refresh sessions to update filter
    } catch (error) {
        console.error('Error saving tattoo:', error);
        alert('Ошибка при сохранении');
    }
}

// Edit tattoo
function editTattoo(tattooId) {
    showTattooForm(tattooId);
}

// Delete tattoo
async function deleteTattoo(tattooId) {
    if (!confirm('Вы уверены, что хотите удалить эту татуировку/татуаж?')) {
        return;
    }
    
    try {
        const response = await fetch(`/api/tattoos/${tattooId}`, {
            method: 'DELETE'
        });
        
        if (!response.ok) {
            throw new Error('Failed to delete tattoo');
        }
        
        await loadTattoos();
        await loadSessions();
        closeTattooModal();
    } catch (error) {
        console.error('Error deleting tattoo:', error);
        alert('Ошибка при удалении');
    }
}

// View tattoo details
function viewTattoo(tattooId) {
    currentTattooId = tattooId;
    const tattoo = tattoos.find(t => t.id === tattooId);
    if (!tattoo) return;
    
    const modal = document.getElementById('tattoo-modal');
    const title = document.getElementById('modal-tattoo-title');
    const body = document.getElementById('modal-tattoo-body');
    
    title.textContent = tattoo.name;
    
    body.innerHTML = `
        <div class="detail-row">
            <span class="detail-label">Название</span>
            <span class="detail-value">${escapeHtml(tattoo.name)}</span>
        </div>
        <div class="detail-row">
            <span class="detail-label">Зона удаления</span>
            <span class="detail-value">${escapeHtml(tattoo.removal_zone) || '—'}</span>
        </div>
        <div class="detail-row">
            <span class="detail-label">Коррекций / перекрытий</span>
            <span class="detail-value">${escapeHtml(tattoo.corrections_count) || '—'}</span>
        </div>
        <div class="detail-row">
            <span class="detail-label">Последний пигмент</span>
            <span class="detail-value">${formatDate(tattoo.last_pigment_date)}</span>
        </div>
        <div class="detail-row">
            <span class="detail-label">Удалял лазером ранее</span>
            <span class="detail-value">${tattoo.no_laser_before ? 'Нет' : 'Да'}</span>
        </div>
        ${!tattoo.no_laser_before ? `
        <div class="detail-row">
            <span class="detail-label">Последнее удаление лазером</span>
            <span class="detail-value">${formatDate(tattoo.last_laser_date)}</span>
        </div>
        <div class="detail-row">
            <span class="detail-label">Где удаляли</span>
            <span class="detail-value">${escapeHtml(tattoo.previous_removal_place) || '—'}</span>
        </div>
        ` : ''}
        <div class="detail-row">
            <span class="detail-label">Желаемый результат</span>
            <span class="detail-value">${escapeHtml(tattoo.desired_result) || '—'}</span>
        </div>
    `;
    
    modal.classList.remove('hidden');
}

// Close tattoo modal
function closeTattooModal(event) {
    if (event && event.target !== event.currentTarget) return;
    const modal = document.getElementById('tattoo-modal');
    modal.classList.add('hidden');
}

// Edit tattoo from modal
function editTattooFromModal() {
    const tattooId = currentTattooId;
    closeTattooModal();
    if (tattooId) {
        editTattoo(tattooId);
    }
}

// Delete tattoo from modal
function deleteTattooFromModal() {
    const tattooId = currentTattooId;
    if (tattooId) {
        deleteTattoo(tattooId);
    }
}

// ============== Sessions Management ==============

// Load sessions data
async function loadSessions() {
    try {
        const tattooFilter = document.getElementById('sessions-tattoo-filter').value;
        
        const params = new URLSearchParams({
            sort_by: sessionsSortBy,
            sort_order: sessionsSortOrder
        });
        if (tattooFilter) params.append('tattoo_filter', tattooFilter);
        
        const response = await fetch(`/api/clients/${CLIENT_ID}/sessions?${params}`);
        
        if (response.status === 401) {
            window.location.href = '/login';
            return;
        }
        
        if (!response.ok) {
            throw new Error('Failed to load sessions');
        }
        
        const data = await response.json();
        sessions = data.sessions;
        totalFlashes = data.total_flashes;
        clientName = data.client_name;
        
        // Update UI
        document.getElementById('client-name').textContent = clientName;
        document.getElementById('client-link').href = '#';
        document.getElementById('client-link').onclick = () => {
            sessionStorage.setItem('openClientModal', CLIENT_ID);
            window.location.href = '/';
        };
        document.getElementById('total-flashes').textContent = totalFlashes.toLocaleString('ru-RU');
        
        // Update flashes label based on filter
        const flashesLabel = document.getElementById('flashes-label');
        if (tattooFilter) {
            flashesLabel.textContent = `Вспышки (${tattooFilter}):`;
        } else {
            flashesLabel.textContent = 'Итог по вспышкам:';
        }
        
        renderSessionsTable();
        updatePartsSelect();
        updateLaserParamsSelects();
        
        // Update tattoo filter dropdown (only when not filtering)
        if (!tattooFilter) {
            updateTattooFilterSelect();
        }
    } catch (error) {
        console.error('Error loading sessions:', error);
    }
}

// Get unique tattoo names from sessions (for backwards compatibility)
function getUniqueTattooNames() {
    const names = new Set();
    sessions.forEach(s => {
        if (s.tattoo_name && s.tattoo_name.trim()) {
            names.add(s.tattoo_name.trim());
        }
    });
    // Also add names from tattoos table
    tattoos.forEach(t => {
        if (t.name && t.name.trim()) {
            names.add(t.name.trim());
        }
    });
    return Array.from(names).sort();
}

// Update tattoo filter dropdown
function updateTattooFilterSelect() {
    const filterSelect = document.getElementById('sessions-tattoo-filter');
    const currentValue = filterSelect.value;
    
    allTattooNames = getUniqueTattooNames();
    
    filterSelect.innerHTML = '<option value="">Все татуировки/татуаж</option>' +
        allTattooNames.map(name => 
            `<option value="${escapeHtml(name)}">${escapeHtml(name)}</option>`
        ).join('');
    
    if (currentValue) {
        filterSelect.value = currentValue;
    }
}

// ============== Custom Autocomplete System ==============

// Store unique values for autocomplete fields
const autocompleteData = {
    'session-part': [],
    'session-wavelength': [],
    'session-diameter': [],
    'session-density': [],
    'session-hertz': []
};

// Currently highlighted item index
let highlightedIndex = -1;
let activeDropdown = null;

// Generic function to get unique values for a field from sessions
function getUniqueFieldValues(fieldName) {
    const values = new Set();
    sessions.forEach(s => {
        const val = s[fieldName];
        if (val !== null && val !== undefined && String(val).trim()) {
            values.add(String(val).trim());
        }
    });
    return Array.from(values).sort((a, b) => {
        const numA = parseFloat(a);
        const numB = parseFloat(b);
        if (!isNaN(numA) && !isNaN(numB)) {
            return numA - numB;
        }
        return a.localeCompare(b);
    });
}

// Update autocomplete data from sessions
function updateAutocompleteData() {
    autocompleteData['session-part'] = getUniqueFieldValues('sub_session');
    autocompleteData['session-wavelength'] = getUniqueFieldValues('wavelength');
    autocompleteData['session-diameter'] = getUniqueFieldValues('diameter');
    autocompleteData['session-density'] = getUniqueFieldValues('density');
    autocompleteData['session-hertz'] = getUniqueFieldValues('hertz');
}

// Show dropdown with filtered options
function showAutocompleteDropdown(inputId) {
    const input = document.getElementById(inputId);
    const dropdown = document.getElementById(`${inputId}-dropdown`);
    if (!input || !dropdown) return;
    
    const query = input.value.toLowerCase().trim();
    const allValues = autocompleteData[inputId] || [];
    
    // Filter values based on input
    const filtered = query 
        ? allValues.filter(v => v.toLowerCase().includes(query))
        : allValues;
    
    if (filtered.length === 0) {
        dropdown.innerHTML = '<div class="autocomplete-empty">Нет вариантов</div>';
    } else {
        dropdown.innerHTML = filtered.map((val, index) => 
            `<div class="autocomplete-item" data-value="${escapeHtml(val)}" data-index="${index}">${escapeHtml(val)}</div>`
        ).join('');
    }
    
    dropdown.classList.remove('hidden');
    activeDropdown = inputId;
    highlightedIndex = -1;
}

// Hide dropdown
function hideAutocompleteDropdown(inputId) {
    const dropdown = document.getElementById(`${inputId}-dropdown`);
    if (dropdown) {
        dropdown.classList.add('hidden');
    }
    if (activeDropdown === inputId) {
        activeDropdown = null;
        highlightedIndex = -1;
    }
}

// Hide all dropdowns
function hideAllAutocompleteDropdowns() {
    Object.keys(autocompleteData).forEach(inputId => {
        hideAutocompleteDropdown(inputId);
    });
}

// Select an item from dropdown
function selectAutocompleteItem(inputId, value) {
    const input = document.getElementById(inputId);
    if (input) {
        input.value = value;
    }
    hideAutocompleteDropdown(inputId);
}

// Handle keyboard navigation
function handleAutocompleteKeydown(e, inputId) {
    const dropdown = document.getElementById(`${inputId}-dropdown`);
    if (!dropdown || dropdown.classList.contains('hidden')) {
        if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
            showAutocompleteDropdown(inputId);
            e.preventDefault();
        }
        return;
    }
    
    const items = dropdown.querySelectorAll('.autocomplete-item');
    if (items.length === 0) return;
    
    if (e.key === 'ArrowDown') {
        e.preventDefault();
        highlightedIndex = Math.min(highlightedIndex + 1, items.length - 1);
        updateHighlight(items);
    } else if (e.key === 'ArrowUp') {
        e.preventDefault();
        highlightedIndex = Math.max(highlightedIndex - 1, 0);
        updateHighlight(items);
    } else if (e.key === 'Enter') {
        e.preventDefault();
        if (highlightedIndex >= 0 && items[highlightedIndex]) {
            selectAutocompleteItem(inputId, items[highlightedIndex].dataset.value);
        } else {
            hideAutocompleteDropdown(inputId);
        }
    } else if (e.key === 'Escape') {
        hideAutocompleteDropdown(inputId);
    }
}

// Update visual highlight
function updateHighlight(items) {
    items.forEach((item, index) => {
        item.classList.toggle('highlighted', index === highlightedIndex);
        if (index === highlightedIndex) {
            item.scrollIntoView({ block: 'nearest' });
        }
    });
}

// Initialize autocomplete for an input
function initAutocomplete(inputId) {
    const input = document.getElementById(inputId);
    const dropdown = document.getElementById(`${inputId}-dropdown`);
    if (!input || !dropdown) return;
    
    // Show on focus
    input.addEventListener('focus', () => {
        showAutocompleteDropdown(inputId);
    });
    
    // Filter on input
    input.addEventListener('input', () => {
        showAutocompleteDropdown(inputId);
    });
    
    // Keyboard navigation
    input.addEventListener('keydown', (e) => {
        handleAutocompleteKeydown(e, inputId);
    });
    
    // Click on dropdown item
    dropdown.addEventListener('mousedown', (e) => {
        const item = e.target.closest('.autocomplete-item');
        if (item) {
            e.preventDefault();
            selectAutocompleteItem(inputId, item.dataset.value);
        }
    });
    
    // Hide on blur (with delay to allow click)
    input.addEventListener('blur', () => {
        setTimeout(() => hideAutocompleteDropdown(inputId), 150);
    });
}

// Legacy functions for compatibility
function updatePartsSelect() {
    updateAutocompleteData();
}

function updateLaserParamsSelects() {
    updateAutocompleteData();
}

function getPartValue() {
    const input = document.getElementById('session-part');
    return input ? (input.value.trim() || null) : null;
}

function setPartValue(value) {
    const input = document.getElementById('session-part');
    if (input) {
        input.value = value || '';
    }
}

// ============== Session Number Auto-fill ==============

function getNextSessionNumberForTattoo(tattooId) {
    if (!tattooId) return 1;
    
    // Get tattoo name
    const tattoo = tattoos.find(t => t.id == tattooId);
    const tattooName = tattoo ? tattoo.name : null;
    
    if (!tattooName) return 1;
    
    // Find sessions with this tattoo (by ID or name for backwards compatibility)
    const tattooSessions = sessions.filter(s => 
        s.tattoo_id == tattooId || 
        (s.tattoo_name && s.tattoo_name.toLowerCase() === tattooName.toLowerCase())
    );
    
    if (tattooSessions.length === 0) return 1;
    const maxNum = Math.max(...tattooSessions.map(s => s.session_number || 0));
    return maxNum + 1;
}

function handleTattooSelectChange() {
    const select = document.getElementById('session-tattoo-select');
    const sessionNumberInput = document.getElementById('session-number');
    const sessionIdInput = document.getElementById('session-id');
    
    // Only auto-fill session number for new sessions
    if (!sessionIdInput.value && select.value) {
        const nextNum = getNextSessionNumberForTattoo(select.value);
        sessionNumberInput.value = nextNum;
    }
}

// ============== Session Sorting ==============

function handleSessionsSort(field) {
    if (sessionsSortBy === field) {
        sessionsSortOrder = sessionsSortOrder === 'asc' ? 'desc' : 'asc';
    } else {
        sessionsSortBy = field;
        sessionsSortOrder = 'asc';
    }
    loadSessions();
}

// ============== Sessions Table ==============

function renderSessionsTable() {
    const tbody = document.getElementById('sessions-table-body');
    const emptyState = document.getElementById('sessions-empty');
    const tableContainer = document.getElementById('sessions-table-container');
    
    if (sessions.length === 0) {
        tableContainer.classList.add('hidden');
        emptyState.classList.remove('hidden');
        return;
    }
    
    tableContainer.classList.remove('hidden');
    emptyState.classList.add('hidden');
    
    tbody.innerHTML = sessions.map(session => `
        <tr onclick="viewSession(${session.id})" style="cursor: pointer;">
            <td><strong>${formatTattooName(session)}</strong></td>
            <td>${session.session_number || '—'}</td>
            <td>${formatDate(session.session_date)}</td>
            <td>${session.wavelength || '—'}</td>
            <td>${session.diameter || '—'}</td>
            <td>${session.density || '—'}</td>
            <td>${session.hertz || '—'}</td>
            <td><strong>${session.flashes_count || 0}</strong></td>
            <td>${session.break_period || '—'}</td>
            <td onclick="event.stopPropagation()">
                <div class="actions-cell">
                    <button class="btn-icon btn-icon-edit" onclick="editSession(${session.id})" title="Редактировать">✏️</button>
                    <button class="btn-icon btn-icon-delete" onclick="deleteSession(${session.id})" title="Удалить">🗑️</button>
                </div>
            </td>
        </tr>
    `).join('');
}

// ============== Session Form ==============

function showSessionForm(sessionId = null) {
    currentSessionId = sessionId;
    const modal = document.getElementById('session-form-modal');
    const form = document.getElementById('session-form');
    const title = document.getElementById('session-form-title');
    
    form.reset();
    
    // Reset zone select and update tattoo list with all tattoos
    document.getElementById('session-zone-select').value = '';
    updateTattooSelect(null);
    
    if (sessionId) {
        title.textContent = 'Редактировать сеанс';
        const session = sessions.find(s => s.id === sessionId);
        if (session) {
            document.getElementById('session-id').value = session.id;
            
            // Set tattoo and zone - try by ID first, then by name
            let tattoo = null;
            if (session.tattoo_id) {
                tattoo = tattoos.find(t => t.id == session.tattoo_id);
                document.getElementById('session-tattoo-select').value = session.tattoo_id;
            } else if (session.tattoo_name) {
                // Find tattoo by name
                tattoo = tattoos.find(t => t.name === session.tattoo_name);
                if (tattoo) {
                    document.getElementById('session-tattoo-select').value = tattoo.id;
                }
            }
            
            // Set zone from tattoo if available
            if (tattoo && tattoo.removal_zone) {
                document.getElementById('session-zone-select').value = tattoo.removal_zone;
            }
            
            document.getElementById('session-number').value = session.session_number || '';
            setPartValue(session.sub_session);
            document.getElementById('session-wavelength').value = session.wavelength || '';
            document.getElementById('session-diameter').value = session.diameter || '';
            document.getElementById('session-density').value = session.density || '';
            document.getElementById('session-hertz').value = session.hertz || '';
            document.getElementById('session-flashes').value = session.flashes_count || '';
            document.getElementById('session-date').value = formatDateForInput(session.session_date);
            document.getElementById('session-break').value = session.break_period || '';
            document.getElementById('session-comment').value = session.comment || '';
        }
    } else {
        title.textContent = 'Добавить сеанс';
        document.getElementById('session-id').value = '';
        document.getElementById('session-date').value = new Date().toISOString().split('T')[0];
        document.getElementById('session-zone-select').value = '';
        document.getElementById('session-tattoo-select').value = '';
        document.getElementById('session-number').value = '';
        setPartValue('');
    }
    
    modal.classList.remove('hidden');
}

function hideSessionForm(event) {
    if (event && event.target !== event.currentTarget) return;
    const modal = document.getElementById('session-form-modal');
    modal.classList.add('hidden');
    currentSessionId = null;
}

function editSession(sessionId) {
    showSessionForm(sessionId);
}

async function deleteSession(sessionId) {
    if (!confirm('Вы уверены, что хотите удалить этот сеанс?')) {
        return;
    }
    
    try {
        const response = await fetch(`/api/sessions/${sessionId}`, {
            method: 'DELETE'
        });
        
        if (!response.ok) {
            throw new Error('Failed to delete session');
        }
        
        await loadSessions();
        closeSessionModal();
    } catch (error) {
        console.error('Error deleting session:', error);
        alert('Ошибка при удалении сеанса');
    }
}

function viewSession(sessionId) {
    currentSessionId = sessionId;
    const session = sessions.find(s => s.id === sessionId);
    if (!session) return;
    
    const modal = document.getElementById('session-modal');
    const title = document.getElementById('modal-session-title');
    const body = document.getElementById('modal-session-body');
    
    const tattooName = session.tattoo_name || getTattooNameById(session.tattoo_id);
    title.textContent = tattooName ? `Сеанс: ${tattooName}` : `Сеанс #${session.session_number || session.id}`;
    
    body.innerHTML = `
        <div class="detail-row">
            <span class="detail-label">Татуировка/Татуаж</span>
            <span class="detail-value">${tattooName || '—'}</span>
        </div>
        <div class="detail-row">
            <span class="detail-label">№ сеанса</span>
            <span class="detail-value">${session.session_number || '—'}</span>
        </div>
        <div class="detail-row">
            <span class="detail-label">Участок</span>
            <span class="detail-value">${session.sub_session || '—'}</span>
        </div>
        <div class="detail-row">
            <span class="detail-label">Дата сеанса</span>
            <span class="detail-value">${formatDate(session.session_date)}</span>
        </div>
        <div class="detail-row">
            <span class="detail-label">Длина волны</span>
            <span class="detail-value">${session.wavelength || '—'}</span>
        </div>
        <div class="detail-row">
            <span class="detail-label">Диаметр</span>
            <span class="detail-value">${session.diameter || '—'}</span>
        </div>
        <div class="detail-row">
            <span class="detail-label">Плотность</span>
            <span class="detail-value">${session.density || '—'}</span>
        </div>
        <div class="detail-row">
            <span class="detail-label">Герц</span>
            <span class="detail-value">${session.hertz || '—'}</span>
        </div>
        <div class="detail-row">
            <span class="detail-label">Вспышки</span>
            <span class="detail-value"><strong>${session.flashes_count || 0}</strong></span>
        </div>
        <div class="detail-row">
            <span class="detail-label">Перерыв</span>
            <span class="detail-value">${session.break_period || '—'}</span>
        </div>
        <div class="detail-row">
            <span class="detail-label">Комментарий</span>
            <span class="detail-value">${session.comment || '—'}</span>
        </div>
    `;
    
    modal.classList.remove('hidden');
}

function closeSessionModal(event) {
    if (event && event.target !== event.currentTarget) return;
    const modal = document.getElementById('session-modal');
    modal.classList.add('hidden');
}

function editSessionFromModal() {
    const sessionId = currentSessionId;
    closeSessionModal();
    if (sessionId) {
        editSession(sessionId);
    }
}

function deleteSessionFromModal() {
    const sessionId = currentSessionId;
    if (sessionId) {
        deleteSession(sessionId);
    }
}

async function saveSession(event) {
    event.preventDefault();
    
    const sessionId = document.getElementById('session-id').value;
    const tattooSelectValue = document.getElementById('session-tattoo-select').value;
    
    // Get tattoo name from the selected tattoo
    let tattooName = null;
    let tattooId = null;
    if (tattooSelectValue) {
        tattooId = parseInt(tattooSelectValue);
        const tattoo = tattoos.find(t => t.id === tattooId);
        tattooName = tattoo ? tattoo.name : null;
    }
    
    const sessionData = {
        tattoo_id: tattooId,
        tattoo_name: tattooName,
        session_number: parseInt(document.getElementById('session-number').value) || null,
        sub_session: getPartValue(),
        wavelength: document.getElementById('session-wavelength').value.trim() || null,
        diameter: document.getElementById('session-diameter').value.trim() || null,
        density: document.getElementById('session-density').value.trim() || null,
        hertz: document.getElementById('session-hertz').value.trim() || null,
        flashes_count: parseInt(document.getElementById('session-flashes').value) || null,
        session_date: document.getElementById('session-date').value || null,
        break_period: document.getElementById('session-break').value || null,
        comment: document.getElementById('session-comment').value || null
    };
    
    try {
        let response;
        if (sessionId) {
            response = await fetch(`/api/sessions/${sessionId}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(sessionData)
            });
        } else {
            response = await fetch(`/api/clients/${CLIENT_ID}/sessions`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(sessionData)
            });
        }
        
        if (!response.ok) {
            throw new Error('Failed to save session');
        }
        
        hideSessionForm();
        await loadSessions();
        updatePartsSelect();
    } catch (error) {
        console.error('Error saving session:', error);
        alert('Ошибка при сохранении сеанса');
    }
}

// ============== Event Listeners ==============

document.addEventListener('DOMContentLoaded', () => {
    // Load initial data
    loadTattoos().then(() => {
        loadSessions();
    });
    
    // Initialize autocomplete fields
    ['session-part', 'session-wavelength', 'session-diameter', 'session-density', 'session-hertz'].forEach(initAutocomplete);
    
    // Form submit handlers
    document.getElementById('session-form').addEventListener('submit', saveSession);
    document.getElementById('tattoo-form').addEventListener('submit', saveTattoo);
    
    // Tattoo filter
    document.getElementById('sessions-tattoo-filter').addEventListener('change', loadSessions);
    
    // Table sorting
    document.querySelectorAll('.sessions-table th.sortable').forEach(th => {
        th.addEventListener('click', () => {
            handleSessionsSort(th.dataset.sort);
        });
    });
    
    // Zone select change (filter tattoos)
    document.getElementById('session-zone-select').addEventListener('change', handleZoneSelectChange);
    
    // Tattoo select change (auto session number and zone)
    document.getElementById('session-tattoo-select').addEventListener('change', function() {
        handleTattooSelectChange();
        handleTattooSelectChangeForZone();
    });
    
    
    // Tattoo form - no laser checkbox
    document.getElementById('tattoo-no-laser').addEventListener('change', toggleTattooLaserFields);
    
    // Escape key to close modals
    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape') {
            const modals = [
                'session-form-modal',
                'session-modal',
                'tattoo-form-modal',
                'tattoo-modal'
            ];
            
            for (const modalId of modals) {
                const modal = document.getElementById(modalId);
                if (!modal.classList.contains('hidden')) {
                    modal.classList.add('hidden');
                    break;
                }
            }
        }
    });
    
    // Close export menus when clicking outside
    document.addEventListener('click', function(e) {
        if (!e.target.closest('.export-dropdown')) {
            document.querySelectorAll('.export-menu').forEach(menu => {
                menu.classList.add('hidden');
            });
        }
        // Close autocomplete dropdowns when clicking outside
        if (!e.target.closest('.autocomplete-wrapper')) {
            hideAllAutocompleteDropdowns();
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
    if (entity === 'sessions') {
        url = `/api/clients/${CLIENT_ID}/export/sessions?format=${format}`;
    } else if (entity === 'tattoos') {
        url = `/api/clients/${CLIENT_ID}/export/tattoos?format=${format}`;
    }
    
    if (url) {
        window.location.href = url;
    }
    
    // Close the menu
    document.querySelectorAll('.export-menu').forEach(menu => {
        menu.classList.add('hidden');
    });
}
