/* ==========================================================================
   ACO Thermal Printer Terminal - Main JavaScript Application
   ========================================================================== */

// Local state management
let activePresetImage = 'receipt';
let activeErrorState = 'None';
let lastSeenJobId = null;
let reconnectTimerInterval = null;
let reconnectProgress = 0;
let reconnectTotalTime = 0;

// Default receipt templates for TR and EN
const receiptTemplates = {
    tr: `--------------------------------
         ACO RECYCLING          
 reverse vending recycling systems
--------------------------------
MachineID: ACO-TEST-0001-0001
16 Eylül 2025 16:19:02 UTC
Aco Recycling Default Reward

Reward: 3.00 ₺

Product       Quantity    Reward
Glass            0         0
Plastic          2         2
Metal            1         1
Tetrapak         0         0
--------------------------------`,
    en: `--------------------------------
         ACO RECYCLING          
 reverse vending recycling systems
--------------------------------
MachineID: ACO-TEST-0001-0001
16 September 2025 16:19:02 UTC
Aco Recycling Default Reward

Reward: 3.00 ₺

Product       Quantity    Reward
Glass            0         0
Plastic          2         2
Metal            1         1
Tetrapak         0         0
--------------------------------`
};

function updateLanguageTemplate() {
    const lang = document.getElementById('select-language').value;
    const textArea = document.getElementById('textarea-text');
    if (textArea) {
        textArea.value = receiptTemplates[lang] || receiptTemplates['tr'];
    }
}

// Load API Token from Local Storage
document.addEventListener('DOMContentLoaded', () => {
    const savedToken = localStorage.getItem('aco_api_token') || 'aco-secret-token';
    document.getElementById('input-api-token').value = savedToken;
    generateRandomJobId();
    
    // Start real-time status polling (every 1 second)
    pollStatus();
    setInterval(pollStatus, 1000);

    // Initial logs fetch
    fetchLogs();

    // Setup language change listener
    const selectLanguage = document.getElementById('select-language');
    if (selectLanguage) {
        selectLanguage.addEventListener('change', updateLanguageTemplate);
        // Make sure the default is set initially
        updateLanguageTemplate();
    }
});

// Save token locally
function saveTokenLocal() {
    const token = document.getElementById('input-api-token').value.trim();
    localStorage.setItem('aco_api_token', token);
    
    const badge = document.getElementById('auth-warning-badge');
    if (token) {
        badge.classList.remove('hidden');
    } else {
        badge.classList.add('hidden');
    }
}

// Generate random job ID
function generateRandomJobId() {
    const chars = 'abcdefghijklmnopqrstuvwxyz0123456789';
    let id = '';
    for (let i = 0; i < 8; i++) {
        id += chars.charAt(Math.floor(Math.random() * chars.length));
    }
    document.getElementById('input-job-id').value = id;
}

// Tab switcher
function openTab(evt, tabId) {
    const tabContents = document.getElementsByClassName('tab-content');
    for (let i = 0; i < tabContents.length; i++) {
        tabContents[i].classList.remove('active');
    }

    const tabLinks = document.getElementsByClassName('tab-link');
    for (let i = 0; i < tabLinks.length; i++) {
        tabLinks[i].classList.remove('active');
    }

    document.getElementById(tabId).classList.add('active');
    evt.currentTarget.classList.add('active');
}

// Select Image Presets
function selectImagePreset(preset) {
    activePresetImage = preset;
    const presets = document.getElementsByClassName('btn-image-preset');
    for (let i = 0; i < presets.length; i++) {
        presets[i].classList.remove('active');
    }
    event.currentTarget.classList.add('active');
}

// Get API Headers
function getHeaders() {
    const token = document.getElementById('input-api-token').value.trim();
    const headers = {
        'Content-Type': 'application/json'
    };
    if (token) {
        headers['X-Api-Token'] = token;
    }
    return headers;
}

// Call API Connect
async function connectPrinter(mode) {
    try {
        const response = await fetch('/api/printer/connect', {
            method: 'POST',
            headers: getHeaders(),
            body: JSON.stringify({ mode })
        });
        
        const data = await response.json();
        if (response.ok) {
            showToast('Bağlantı Kuruldu', `${mode.toUpperCase()} bağlantısı aktif.`, 'success');
        } else {
            showToast('Hata', data.error || 'Bağlantı başarısız.', 'error');
        }
    } catch (err) {
        showToast('Hata', 'Sunucu bağlantı hatası.', 'error');
    }
}

// Call API Disconnect
async function disconnectPrinter() {
    try {
        const response = await fetch('/api/printer/connect', {
            method: 'POST',
            headers: getHeaders(),
            body: JSON.stringify({ mode: 'none' })
        });
        
        if (response.ok) {
            showToast('Bağlantı Kesildi', 'Yazıcı bağlantısı kapatıldı.', 'success');
        }
    } catch (err) {
        showToast('Hata', 'Sunucu hatası.', 'error');
    }
}

// Trigger Simulated Error on Simulator
async function simulateError(errorCode) {
    try {
        const response = await fetch('/api/simulator/error', {
            method: 'POST',
            headers: getHeaders(),
            body: JSON.stringify({ errorCode })
        });
        
        if (response.ok) {
            activeErrorState = errorCode;
            updateSimulatorUI(errorCode);
            if (errorCode !== 'None') {
                showToast('Hata Tetiklendi', `Sanal donanım hatası aktif: ${errorCode}`, 'error');
            } else {
                showToast('Hata Temizlendi', 'Sanal donanım hatası temizlendi. Cihaz normale döndü.', 'success');
            }
        }
    } catch (err) {
        showToast('Hata', 'Hata simüle edilemedi.', 'error');
    }
}

// Print Actions
async function printText() {
    const text = document.getElementById('textarea-text').value;
    const lang = document.getElementById('select-language').value;
    const jobId = document.getElementById('input-job-id').value.trim();

    sendPrintRequest('/api/printer/print/text', { text, language: lang, jobId });
}

async function printImage() {
    const lang = document.getElementById('select-language').value;
    const jobId = document.getElementById('input-job-id').value.trim();
    // Simulate image base64 based on selected preset
    const presetLabels = {
        'receipt': 'BASE64_ACO_RECYCLING_LOGO_PATTERN',
        'recycle': 'BASE64_GREEN_RECYCLE_SYMBOL_PATTERN',
        'smile': 'BASE64_THANK_YOU_EMOJI_PATTERN'
    };
    const mockBase64 = presetLabels[activePresetImage];

    sendPrintRequest('/api/printer/print/image', { imageBase64: mockBase64, language: lang, jobId });
}

async function printQr() {
    const qrData = document.getElementById('input-qr-data').value.trim();
    const lang = document.getElementById('select-language').value;
    const jobId = document.getElementById('input-job-id').value.trim();

    sendPrintRequest('/api/printer/print/qr', { qrData, language: lang, jobId });
}

// Reprint Failed Job
async function reprintJob(jobId) {
    try {
        const response = await fetch('/api/printer/reprint', {
            method: 'POST',
            headers: getHeaders(),
            body: JSON.stringify({ jobId })
        });
        const data = await response.json();
        if (response.ok) {
            showToast('Yeniden Basılıyor', `İş tekrar kuyruğa alındı: ID ${jobId}`, 'success');
            generateRandomJobId();
        } else {
            showToast('Reprint Hatası', data.error || 'İş basılamadı.', 'error');
        }
    } catch (err) {
        showToast('Hata', 'Sunucu hatası.', 'error');
    }
}

// Send Print Request helper
async function sendPrintRequest(url, body) {
    try {
        const response = await fetch(url, {
            method: 'POST',
            headers: getHeaders(),
            body: JSON.stringify(body)
        });
        
        const job = await response.json();
        
        if (response.ok) {
            if (job.status === 'Completed') {
                showToast('Yazdırıldı', `Baskı tamamlandı! (Süre: ${job.executionDurationMs}ms)`, 'success');
            } else if (job.status === 'Pending') {
                showToast('Kuyrukta', `Baskı işleme alındı. Kuyruğa eklendi.`, 'success');
            } else if (job.status === 'Failed') {
                showToast('Baskı Hatası', `Hata: ${job.errorCode} - ${job.errorDetail}`, 'error');
            }
            // Generate a new random job ID automatically for next print to ensure seamless testing
            generateRandomJobId();
        } else {
            showToast('Hata', job.error || 'Baskı isteği gönderilemedi.', 'error');
        }
    } catch (err) {
        showToast('Hata', 'Baskı sunucusu bağlantı hatası.', 'error');
    }
}

// Update UI buttons based on active simulated error
function updateSimulatorUI(errorCode) {
    const buttons = document.querySelectorAll('.btn-sim-error');
    buttons.forEach(btn => {
        if (btn.getAttribute('data-error') === errorCode) {
            btn.classList.add('active');
        } else {
            btn.classList.remove('active');
        }
    });
}

// Poll printer status
async function pollStatus() {
    try {
        const response = await fetch('/api/printer/status');
        if (!response.ok) return;

        const status = await response.json();
        
        // Update connection status badge
        const dot = document.getElementById('status-pulse-dot');
        const stateText = document.getElementById('txt-connection-state');
        const modeText = document.getElementById('txt-connection-mode');

        dot.className = 'status-dot';
        modeText.innerText = status.connectionMode.toUpperCase();

        if (status.connectionState === 'Connected') {
            dot.classList.add('connected');
            stateText.innerText = 'Çevrimiçi';
            hideReconnectCountdown();
        } else if (status.connectionState === 'Connecting') {
            dot.classList.add('connecting');
            stateText.innerText = 'Bağlanıyor...';
            showReconnectCountdown();
        } else {
            dot.classList.add('disconnected');
            stateText.innerText = 'Çevrimdışı';
            // Show countdown if communication error is active and mode is set
            if (status.activeError === 'COMM_ERROR' && status.connectionMode !== 'none') {
                showReconnectCountdown();
            } else {
                hideReconnectCountdown();
            }
        }

        // Update hardware error states on simulator buttons
        if (status.activeError !== activeErrorState) {
            activeErrorState = status.activeError;
            updateSimulatorUI(status.activeError);
        }

        // Update LED indicators
        const indPower = document.getElementById('ind-power');
        const indError = document.getElementById('ind-error');
        const indPaper = document.getElementById('ind-paper');

        indPower.className = 'indicator-led green';
        indError.className = 'indicator-led';
        indPaper.className = 'indicator-led';

        if (status.activeError !== 'None') {
            indError.classList.add('red');
            if (status.activeError === 'PAPER_OUT') {
                indPaper.classList.add('red');
            }
        }

        // Update Roll Prediction Widget
        document.getElementById('txt-paper-percentage').innerText = `${status.paperPercentage}%`;
        document.getElementById('txt-paper-length').innerText = `${status.remainingPaperCm} cm`;
        document.getElementById('txt-estimated-prints').innerText = `${status.estimatedPrintsLeft} adet`;
        document.getElementById('txt-total-printed').innerText = `${status.totalPrintedCount} adet`;

        // Update circle fill
        const circle = document.getElementById('roll-circle-fill');
        circle.setAttribute('stroke-dasharray', `${status.paperPercentage}, 100`);

        // Check if there is a new successfully completed print job
        if (status.lastJob && status.lastJob.jobId !== lastSeenJobId) {
            lastSeenJobId = status.lastJob.jobId;
            renderReceipt(status.lastJob);
        }

        // Update Queue Counter
        document.getElementById('badge-queue-count').innerText = status.queueLength;
        document.getElementById('txt-queue-eta').innerText = `${status.queueTotalDurationMs}ms`;

        // Render Queue Table
        renderQueueTable(status.pendingJobs, status.failedJobs);

        // Fetch logs to keep log terminal real-time
        fetchLogs();

    } catch (err) {
        console.error('Error polling printer status', err);
    }
}

// Render queue and failed jobs in table
function renderQueueTable(pendingJobs, failedJobs) {
    const tbody = document.getElementById('table-queue-body');
    
    const allJobs = [...pendingJobs, ...failedJobs];
    // Sort so Pending/Processing is first, then newest
    allJobs.sort((a, b) => b.createdAt.localeCompare(a.createdAt));

    if (allJobs.length === 0) {
        tbody.innerHTML = '<tr><td colspan="6" class="empty-row">Kuyrukta aktif veya başarısız iş bulunmuyor.</td></tr>';
        return;
    }

    let rows = '';
    allJobs.forEach(job => {
        const time = new Date(job.createdAt).toLocaleTimeString();
        let statusBadge = `<span class="status-pill ${job.status.toLowerCase()}">${job.status}</span>`;
        let actionButton = '';

        if (job.status === 'Failed') {
            actionButton = `<button class="btn btn-secondary btn-sm" onclick="reprintJob('${job.jobId}')"><i class="fa-solid fa-rotate-right"></i> Tekrar Bastır</button>`;
        } else {
            actionButton = `<span class="helper-text">Kuyrukta bekliyor</span>`;
        }

        rows += `
            <tr>
                <td class="receipt-bold">${job.jobId}</td>
                <td>${job.contentType.toUpperCase()}</td>
                <td>${time}</td>
                <td>${job.executionDurationMs}ms</td>
                <td>${statusBadge}</td>
                <td>${actionButton}</td>
            </tr>
        `;
    });

    tbody.innerHTML = rows;
}

// Show reconnection countdown
function showReconnectCountdown() {
    const container = document.getElementById('reconnect-countdown-container');
    if (container.classList.contains('hidden')) {
        container.classList.remove('hidden');
        reconnectTotalTime = 2; // initial estimate
        reconnectProgress = 0;
        
        if (reconnectTimerInterval) clearInterval(reconnectTimerInterval);
        
        reconnectTimerInterval = setInterval(() => {
            reconnectProgress += 0.2;
            let displaySec = Math.ceil(Math.max(0, reconnectTotalTime - reconnectProgress));
            document.getElementById('reconnect-timer').innerText = `${displaySec}s`;
            
            // Just simulate progress fill up
            let pct = (reconnectProgress / reconnectTotalTime) * 100;
            if (pct >= 100) {
                // Next attempt will increase timing (exponential backoff simulated)
                reconnectProgress = 0;
                reconnectTotalTime = Math.min(reconnectTotalTime * 2, 30);
            }
            document.getElementById('reconnect-progress-bar').style.width = `${Math.min(pct, 100)}%`;
        }, 200);
    }
}

function hideReconnectCountdown() {
    const container = document.getElementById('reconnect-countdown-container');
    container.classList.add('hidden');
    if (reconnectTimerInterval) {
        clearInterval(reconnectTimerInterval);
        reconnectTimerInterval = null;
    }
}

// Render printed ticket inside machine emu
function renderReceipt(job) {
    const container = document.getElementById('receipt-paper-out-container');
    
    // Clear placeholder if present
    const placeholder = container.querySelector('.receipt-placeholder');
    if (placeholder) {
        container.innerHTML = '';
    }

    const isEn = job.language === 'en';
    const dateLabel = isEn ? 'DATE' : 'TARİH';
    const thankYouLabel = isEn ? 'THANK YOU' : 'TEŞEKKÜRLER';

    // Format content inside ticket
    let customTicketContent = '';
    
    if (job.contentType === 'text') {
        customTicketContent = `<pre style="white-space: pre-wrap; font-family: inherit;">${escapeHtml(job.content)}</pre>`;
    } else if (job.contentType === 'qr') {
        const qrTitle = isEn ? 'QR CODE PRINT' : 'QR KOD BASKISI';
        customTicketContent = `
            <div class="receipt-center receipt-bold">${qrTitle}</div>
            <div class="receipt-center" style="margin: 10px 0;">
                <img src="${job.generatedQrCode}" style="width: 120px; height: 120px; border: 1px solid #eee; padding: 4px; background: #fff;" alt="QR Code" />
            </div>
            <div class="receipt-center" style="font-size: 8px; word-break: break-all; line-height: 1.2;">${escapeHtml(job.content)}</div>
        `;
    } else if (job.contentType === 'image') {
        let presetName = isEn ? 'Custom Pattern' : 'Farklı Desen';
        let presetIcon = 'fa-solid fa-shapes';
        
        if (job.content.includes('LOGO')) {
            presetName = isEn ? 'ACO LOGO & SLOGAN' : 'ACO LOGO VE SLOGAN';
            presetIcon = 'fa-solid fa-recycle';
        } else if (job.content.includes('RECYCLE')) {
            presetName = isEn ? 'RECYCLING SYMBOL' : 'GERİ DÖNÜŞÜM SİMGESİ';
            presetIcon = 'fa-solid fa-recycle';
        } else if (job.content.includes('THANK')) {
            presetName = isEn ? 'THANK YOU EMOJI' : 'TEŞEKKÜR EMOJİSİ';
            presetIcon = 'fa-regular fa-face-smile';
        }

        const imageTitle = isEn ? 'IMAGE VISUALIZATION' : 'GÖRSEL GÖRÜNTÜLEME';
        const base64Label = isEn ? '[Base64 Image Data Printed]' : '[Base64 Resim Datası Basıldı]';

        customTicketContent = `
            <div class="receipt-center receipt-bold">${imageTitle}</div>
            <div class="receipt-image-mock">
                <i class="${presetIcon}"></i>
                <span>${presetName}</span>
            </div>
            <div class="receipt-center" style="font-size: 8px;">${base64Label}</div>
        `;
    }

    const ticket = document.createElement('div');
    ticket.className = 'receipt-ticket';
    ticket.innerHTML = `
        <div class="receipt-center receipt-header-title">ACO THERMAL PRINTER</div>
        <div class="receipt-center" style="font-size: 8px; color: #555;">JOB ID: ${job.jobId} | ${dateLabel}: ${new Date(job.createdAt).toLocaleTimeString()}</div>
        <hr>
        ${customTicketContent}
        <hr>
        <div class="receipt-center receipt-bold" style="font-size: 9px;">${thankYouLabel}</div>
        <div class="receipt-center" style="font-size: 7px; color: #555;">ACO TEST DEV - C# .NET 8</div>
    `;

    // Prepend so newest receipt comes out on top sliding down!
    container.insertBefore(ticket, container.firstChild);

    // Limit to 10 tickets displayed to prevent page bloating
    if (container.children.length > 10) {
        container.removeChild(container.lastChild);
    }
}

// Fetch operation JSON logs
async function fetchLogs() {
    try {
        const response = await fetch('/api/logs?limit=30');
        if (!response.ok) return;

        const logs = await response.json();
        const logsContainer = document.getElementById('logs-output-container');
        
        if (logs.length === 0) {
            logsContainer.innerHTML = '<div class="helper-text">Henüz işlem logu bulunmuyor.</div>';
            return;
        }

        let lines = '';
        logs.forEach(log => {
            const statusClass = log.status === 'success' ? 'success' : 'error';
            const logJsonString = JSON.stringify(log);
            lines += `
                <div class="log-line ${statusClass}">
                    <span class="ts">[${log.ts}]</span>${escapeHtml(logJsonString)}
                </div>
            `;
        });

        logsContainer.innerHTML = lines;
    } catch (err) {
        console.error('Error fetching logs', err);
    }
}

// Helper to escape HTML tags to avoid script injections
function escapeHtml(text) {
    if (!text) return '';
    return text
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#039;");
}

// Custom Toast notifications
function showToast(title, message, type) {
    const toast = document.getElementById('error-toast');
    const toastIcon = toast.querySelector('.toast-icon');
    const toastTitle = document.getElementById('toast-title');
    const toastMessage = document.getElementById('toast-message');

    toastTitle.innerText = title;
    toastMessage.innerText = message;

    toast.className = 'toast';
    toastIcon.className = 'toast-icon';

    if (type === 'success') {
        toast.style.borderColor = 'var(--neon-green)';
        toast.style.boxShadow = '0 10px 30px rgba(0,0,0,0.5), 0 0 15px rgba(57, 255, 20, 0.2)';
        toastIcon.className += ' fa-solid fa-circle-check';
        toastIcon.style.color = 'var(--neon-green)';
    } else {
        toast.style.borderColor = 'var(--neon-red)';
        toast.style.boxShadow = '0 10px 30px rgba(0,0,0,0.5), 0 0 15px rgba(255, 56, 96, 0.2)';
        toastIcon.className += ' fa-solid fa-circle-exclamation';
        toastIcon.style.color = 'var(--neon-red)';
    }

    toast.classList.remove('hidden');

    // Auto close after 4 seconds
    setTimeout(() => {
        closeToast();
    }, 4000);
}

function closeToast() {
    const toast = document.getElementById('error-toast');
    toast.classList.add('hidden');
}
