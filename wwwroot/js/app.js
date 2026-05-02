const API_URL = 'http://0.0.0.0:5000/api';

// DOM Elements
const navItems = document.querySelectorAll('.nav-item');
const sections = document.querySelectorAll('.section');
const banModal = document.getElementById('ban-modal');
const addBanBtn = document.getElementById('add-ban-btn');
const banCancelBtn = document.getElementById('ban-cancel');
const banConfirmBtn = document.getElementById('ban-confirm');
const modalClose = document.querySelector('.modal-close');
const banTypeSelect = document.getElementById('ban-type');
const durationGroup = document.getElementById('duration-group');

let currentUsers = [];
let currentBans = [];

// Initialize
document.addEventListener('DOMContentLoaded', () => {
    setupNavigation();
    setupEventListeners();
    loadDashboard();
    loadUsers();
    loadBans();
});

// Navigation
function setupNavigation() {
    navItems.forEach(item => {
        item.addEventListener('click', (e) => {
            e.preventDefault();
            const section = item.dataset.section;
            
            navItems.forEach(i => i.classList.remove('active'));
            item.classList.add('active');
            
            sections.forEach(s => s.classList.remove('active'));
            document.getElementById(section).classList.add('active');
        });
    });
}

function setupEventListeners() {
    addBanBtn.addEventListener('click', openBanModal);
    banCancelBtn.addEventListener('click', closeBanModal);
    modalClose.addEventListener('click', closeBanModal);
    banConfirmBtn.addEventListener('click', confirmBan);
    banTypeSelect.addEventListener('change', updateDurationGroup);
}

function updateDurationGroup() {
    if (banTypeSelect.value === 'permanent') {
        durationGroup.style.display = 'none';
    } else {
        durationGroup.style.display = 'block';
    }
}

// Dashboard
async function loadDashboard() {
    try {
        const usersRes = await fetch(`${API_URL}/admin/users`);
        const bansRes = await fetch(`${API_URL}/admin/bans`);
        
        if (usersRes.ok && bansRes.ok) {
            const users = await usersRes.json();
            const bans = await bansRes.json();
            
            const activeUsers = users.filter(u => u.isActive && !bans.some(b => b.userId === u.id)).length;
            const bannedUsers = users.filter(u => !u.isActive || bans.some(b => b.userId === u.id)).length;
            
            document.getElementById('total-users').textContent = users.length;
            document.getElementById('active-users').textContent = activeUsers;
            document.getElementById('banned-users').textContent = bannedUsers;
        }
    } catch (error) {
        console.error('Error loading dashboard:', error);
    }
}

// Users Management
async function loadUsers() {
    try {
        const res = await fetch(`${API_URL}/admin/users`);
        if (!res.ok) throw new Error('Failed to load users');
        
        currentUsers = await res.json();
        renderUsers();
    } catch (error) {
        console.error('Error loading users:', error);
    }
}

function renderUsers() {
    const tbody = document.getElementById('users-tbody');
    tbody.innerHTML = '';
    
    currentUsers.forEach(user => {
        const row = document.createElement('tr');
        const isBanned = user.isActive === false;
        
        row.innerHTML = `
            <td>${user.id}</td>
            <td>${user.username}</td>
            <td>${user.email}</td>
            <td>${new Date(user.createdAt).toLocaleDateString()}</td>
            <td>${user.lastLogin ? new Date(user.lastLogin).toLocaleString() : 'Nunca'}</td>
            <td>${user.lastIp || 'N/A'}</td>
            <td><span class="status-badge ${isBanned ? 'banned' : 'active'}">${isBanned ? 'Baneado' : 'Activo'}</span></td>
            <td>
                ${isBanned ? `<button class="btn btn-primary btn-sm" onclick="unbanUser(${user.id})">Desbanear</button>` : `<button class="btn btn-danger btn-sm" onclick="banUser(${user.id})">Banear</button>`}
            </td>
        `;
        tbody.appendChild(row);
    });
}

// Ban Management
async function loadBans() {
    try {
        const res = await fetch(`${API_URL}/admin/bans`);
        if (!res.ok) throw new Error('Failed to load bans');
        
        currentBans = await res.json();
        renderBans();
        populateBanUserSelect();
    } catch (error) {
        console.error('Error loading bans:', error);
    }
}

function renderBans() {
    const tbody = document.getElementById('bans-tbody');
    tbody.innerHTML = '';
    
    currentBans.forEach(ban => {
        const row = document.createElement('tr');
        const isExpired = !ban.isPermanent && new Date(ban.expiresAt) < new Date();
        
        row.innerHTML = `
            <td>${ban.id}</td>
            <td>${ban.user?.username || 'Desconocido'}</td>
            <td>${ban.ipAddress}</td>
            <td>${ban.reason}</td>
            <td>${new Date(ban.bannedAt).toLocaleString()}</td>
            <td>${ban.isPermanent ? 'Permanente' : new Date(ban.expiresAt).toLocaleString()}</td>
            <td><span class="status-badge ${isExpired ? 'inactive' : 'banned'}">${isExpired ? 'Expirado' : 'Activo'}</span></td>
            <td>
                <button class="btn btn-primary btn-sm" onclick="unbanById(${ban.id})">Desbanear</button>
            </td>
        `;
        tbody.appendChild(row);
    });
}

function populateBanUserSelect() {
    const select = document.getElementById('ban-user-select');
    select.innerHTML = '<option value="">Selecciona un usuario...</option>';
    
    currentUsers.forEach(user => {
        const option = document.createElement('option');
        option.value = user.id;
        option.textContent = `${user.username} (${user.email})`;
        select.appendChild(option);
    });
}

// Modal Functions
function openBanModal() {
    banModal.classList.add('active');
}

function closeBanModal() {
    banModal.classList.remove('active');
    document.getElementById('ban-user-select').value = '';
    document.getElementById('ban-reason').value = '';
    document.getElementById('ban-type').value = 'hours';
    document.getElementById('ban-duration').value = '24';
    updateDurationGroup();
}

async function confirmBan() {
    const userId = parseInt(document.getElementById('ban-user-select').value);
    const reason = document.getElementById('ban-reason').value;
    const banType = document.getElementById('ban-type').value;
    const duration = parseInt(document.getElementById('ban-duration').value) || 0;
    
    if (!userId || !reason) {
        alert('Por favor completa todos los campos');
        return;
    }
    
    try {
        const durationHours = banType === 'permanent' ? 0 : duration;
        
        const res = await fetch(`${API_URL}/admin/ban`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                userId,
                reason,
                durationHours,
                adminId: 1
            })
        });
        
        if (res.ok) {
            alert('Usuario baneado correctamente');
            closeBanModal();
            loadBans();
            loadUsers();
            loadDashboard();
        } else {
            alert('Error al banear el usuario');
        }
    } catch (error) {
        console.error('Error banning user:', error);
        alert('Error al banear el usuario');
    }
}

async function banUser(userId) {
    document.getElementById('ban-user-select').value = userId;
    openBanModal();
}

async function unbanUser(userId) {
    if (!confirm('¿Desbanear este usuario?')) return;
    
    try {
        const ban = currentBans.find(b => b.userId === userId);
        if (!ban) {
            alert('No se encontró el ban');
            return;
        }
        
        const res = await fetch(`${API_URL}/admin/unban/${ban.id}`, {
            method: 'DELETE'
        });
        
        if (res.ok) {
            alert('Usuario desbaneado correctamente');
            loadBans();
            loadUsers();
            loadDashboard();
        } else {
            alert('Error al desbanear el usuario');
        }
    } catch (error) {
        console.error('Error unbanning user:', error);
        alert('Error al desbanear el usuario');
    }
}

async function unbanById(banId) {
    if (!confirm('¿Desbanear?')) return;
    
    try {
        const res = await fetch(`${API_URL}/admin/unban/${banId}`, {
            method: 'DELETE'
        });
        
        if (res.ok) {
            alert('Ban removido correctamente');
            loadBans();
            loadUsers();
            loadDashboard();
        } else {
            alert('Error al remover el ban');
        }
    } catch (error) {
        console.error('Error removing ban:', error);
        alert('Error al remover el ban');
    }
}