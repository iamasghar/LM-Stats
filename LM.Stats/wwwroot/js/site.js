$(document).ready(function() {
    const importModal = new bootstrap.Modal(document.getElementById('importModal'));
    const confirmBackupModal = new bootstrap.Modal(document.getElementById('confirmBackupModal'));
    const confirmSyncModal = new bootstrap.Modal(document.getElementById('confirmSyncModal'));
    const rollbackModal = new bootstrap.Modal(document.getElementById('rollbackModal'));
    const configSettingsModalEl = document.getElementById('configSettingsModal');
    const configSettingsModal = configSettingsModalEl ? new bootstrap.Modal(configSettingsModalEl) : null;

    const $huntFile = $('#huntFileInput');
    const $killsFile = $('#killsFileInput');
    const $dateRange = $('#dateRange');
    const $uniqueId = $('#uniqueId');
    const $submitImport = $('#submitImport');
    const $status = $('#fileValidationStatus');
    const $rollbackDropdown = $('#rollbackReportDropdown');
    const $cfgHuntGoal = $('#cfgHuntGoal');
    const $cfgPurchaseGoal = $('#cfgPurchaseGoal');
    const $cfgKillsGoal = $('#cfgKillsGoal');
    const $cfgSkipInput = $('#cfgSkipInReportInput');
    const $cfgStatus = $('#configSettingsStatus');
    let isImportSubmitting = false;
    let isConfigSaving = false;
    let cfgSkipTomSelect = null;

    function setConfigStatus(message, type, isLoading) {
        if (!$cfgStatus.length) return;
        if (!message) {
            $cfgStatus.addClass('d-none').removeClass('alert-secondary alert-success alert-danger').html('');
            return;
        }

        $cfgStatus
            .removeClass('d-none alert-secondary alert-success alert-danger')
            .addClass(`alert-${type}`)
            .html(isLoading
                ? `<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>${message}`
                : message);
    }

    function ensureConfigSkipSelector() {
        if (!$cfgSkipInput.length || typeof TomSelect === 'undefined') return null;
        if (cfgSkipTomSelect) return cfgSkipTomSelect;

        cfgSkipTomSelect = new TomSelect('#cfgSkipInReportInput', {
            plugins: {
                remove_button: { title: 'Remove' }
            },
            create: true,
            createOnBlur: true,
            persist: false,
            maxOptions: 5000,
            delimiter: ',',
            hidePlaceholder: false,
            placeholder: 'Type player name and press Enter',
            onItemAdd: function(value) {
                const normalized = String(value || '').trim();
                if (!normalized) return;

                if (normalized !== value) {
                    this.removeItem(value, true);
                    this.addOption({ value: normalized, text: normalized });
                    this.addItem(normalized, true);
                    return;
                }

                this.addOption({ value: normalized, text: normalized });
                const duplicates = this.items.filter(x => x.toLowerCase() === normalized.toLowerCase());
                if (duplicates.length > 1) {
                    this.removeItem(value, true);
                }
            }
        });

        return cfgSkipTomSelect;
    }

    async function hydratePlayerSuggestions() {
        const selector = ensureConfigSkipSelector();
        if (!selector) return;

        try {
            const resp = await fetch('/Report/SearchPlayers?term=&take=5000');
            if (!resp.ok) return;
            const names = await resp.json();
            if (!Array.isArray(names)) return;

            selector.clearOptions();
            names
                .filter(x => typeof x === 'string' && x.trim().length > 0)
                .forEach(name => {
                    const cleaned = String(name).trim();
                    selector.addOption({ value: cleaned, text: cleaned });
                });
            selector.refreshOptions(false);
        } catch {
            // Ignore suggestion fetch failures.
        }
    }

    async function loadConfigSettings() {
        setConfigStatus('Loading configuration...', 'secondary', true);

        try {
            const response = await fetch('/Report/GetConfigSettings');
            if (!response.ok) {
                setConfigStatus('Failed to load configuration.', 'danger', false);
                return;
            }

            const cfg = await response.json();
            $cfgHuntGoal.val(Number(cfg.huntGoal || 0));
            $cfgPurchaseGoal.val(Number(cfg.purchaseGoal || 0));
            $cfgKillsGoal.val(Number(cfg.killsGoal || 0));

            const selector = ensureConfigSkipSelector();
            if (selector) {
                selector.clear(true);
                const loaded = Array.isArray(cfg.skipInReport) ? cfg.skipInReport : [];
                loaded.forEach(name => {
                    const cleaned = String(name || '').trim();
                    if (!cleaned) return;
                    selector.addOption({ value: cleaned, text: cleaned });
                    selector.addItem(cleaned, true);
                });
            }
            setConfigStatus('', 'secondary', false);
        } catch {
            setConfigStatus('Failed to load configuration.', 'danger', false);
        }
    }

    async function saveConfigSettings() {
        if (isConfigSaving) return;

        const huntGoal = Number($cfgHuntGoal.val() || 0);
        const purchaseGoal = Number($cfgPurchaseGoal.val() || 0);
        const killsGoal = Number($cfgKillsGoal.val() || 0);

        if (huntGoal < 0 || purchaseGoal < 0 || killsGoal < 0) {
            setConfigStatus('Goals must be zero or positive values.', 'danger', false);
            return;
        }

        isConfigSaving = true;
        setButtonLoading('#saveConfigSettingsBtn', true, 'Saving...', 'Save Settings');
        setConfigStatus('Saving configuration...', 'secondary', true);

        try {
            const selector = ensureConfigSkipSelector();
            const skipInReport = selector
                ? Array.from(new Set((selector.items || [])
                    .map(x => String(x || '').trim())
                    .filter(Boolean)
                    .map(x => x.toLowerCase())))
                    .map(lower => (selector.items || []).find(x => String(x || '').trim().toLowerCase() === lower))
                : [];

            const response = await fetch('/Report/UpdateConfigSettings', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    huntGoal,
                    purchaseGoal,
                    killsGoal,
                    skipInReport
                })
            });

            const result = await response.json();
            if (!response.ok || !result.success) {
                setConfigStatus(result.message || 'Failed to save configuration.', 'danger', false);
                return;
            }

            setConfigStatus(result.message || 'Configuration updated successfully.', 'success', false);
            showAlert(result.message || 'Configuration updated successfully.', 'success');
            if (configSettingsModal) {
                configSettingsModal.hide();
            }

            if (typeof window.generateReport === 'function') {
                window.generateReport();
            }
        } catch {
            setConfigStatus('Failed to save configuration.', 'danger', false);
        } finally {
            isConfigSaving = false;
            setButtonLoading('#saveConfigSettingsBtn', false, '', 'Save Settings');
        }
    }

    $dateRange.daterangepicker({
        parentEl: '#importModal',
        drops: 'up',
        opens: 'left',
        autoApply: true,
        autoUpdateInput: true,
        locale: {
            format: 'YYYY/MM/DD',
            cancelLabel: 'Clear'
        }
    });

    function setDateRangeValue(from, to) {
        const picker = $dateRange.data('daterangepicker');
        if (!picker) return;

        picker.setStartDate(moment(from));
        picker.setEndDate(moment(to));
        $dateRange.val(`${moment(from).format('YYYY/MM/DD')} - ${moment(to).format('YYYY/MM/DD')}`);
    }

    function updateUniqueId() {
        const value = $dateRange.val();
        if (!value || !value.includes(' - ')) {
            $uniqueId.val('');
            return;
        }

        const [fromRaw, toRaw] = value.split(' - ');
        const from = moment(fromRaw, 'YYYY/MM/DD', true);
        const to = moment(toRaw, 'YYYY/MM/DD', true);
        if (!from.isValid() || !to.isValid()) {
            $uniqueId.val('');
            return;
        }

        const fromNormalized = from.format('YYYYMMDD');
        const toNormalized = to.format('YYYYMMDD');
        $uniqueId.val(`${fromNormalized}_${toNormalized}_${Date.now()}`);
    }

    function setImportControlsEnabled(enabled) {
        $dateRange.prop('disabled', !enabled);
        $submitImport.prop('disabled', !enabled || isImportSubmitting);
        if (!enabled) {
            $dateRange.val('');
            $uniqueId.val('');
        }
    }

    function setValidationStatus(message, type, isLoading) {
        if (!message) {
            $status.addClass('d-none').removeClass('alert-secondary alert-success alert-danger').html('');
            return;
        }

        $status
            .removeClass('d-none alert-secondary alert-success alert-danger')
            .addClass(`alert-${type}`)
            .html(isLoading
                ? `<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>${message}`
                : message);
    }

    function resetImportModalState() {
        setImportControlsEnabled(false);
        setValidationStatus('', 'secondary', false);
        $huntFile.val('');
        $killsFile.val('');
    }

    async function validateSelectedFiles() {
        const hunt = $huntFile[0]?.files?.[0];
        const kills = $killsFile[0]?.files?.[0];

        if (!hunt || !kills) {
            setImportControlsEnabled(false);
            setValidationStatus('Upload both Hunt and Kills files to start validation.', 'secondary', false);
            return;
        }

        setImportControlsEnabled(false);
        setValidationStatus('Validating uploaded files...', 'secondary', true);

        const formData = new FormData();
        formData.append('huntFile', hunt);
        formData.append('killsFile', kills);

        $.ajax({
            url: '/Home/ValidateImportFiles',
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            success: function(response) {
                if (!response.success) {
                    setValidationStatus(response.message || 'Validation failed.', 'danger', false);
                    return;
                }

                const huntRows = response.hunt?.parsedRows ?? 0;
                const killRows = response.kills?.parsedRows ?? 0;
                setValidationStatus(`Validation successful. Hunt rows: ${huntRows}, Kills rows: ${killRows}.`, 'success', false);

                setImportControlsEnabled(true);
                if (response.suggestedFromDate && response.suggestedToDate) {
                    setDateRangeValue(response.suggestedFromDate, response.suggestedToDate);
                }
                updateUniqueId();
            },
            error: function() {
                setValidationStatus('Validation request failed.', 'danger', false);
                setImportControlsEnabled(false);
            }
        });
    }

    function loadRollbackReports() {
        $rollbackDropdown.html('<option value="">Loading...</option>');
        $.ajax({
            url: '/Report/GetUploadedReports',
            type: 'GET',
            success: function(reports) {
                if (!Array.isArray(reports) || reports.length === 0) {
                    $rollbackDropdown.html('<option value="">No uploaded reports found</option>');
                    return;
                }

                const options = ['<option value="">Select report...</option>'];
                reports.forEach(r => {
                    options.push(`<option value="${r.id}">${r.displayName}</option>`);
                });
                $rollbackDropdown.html(options.join(''));
            },
            error: function() {
                $rollbackDropdown.html('<option value="">Failed to load reports</option>');
            }
        });
    }

    // Import button click
    $('#importBtn').click(function() {
        resetImportModalState();
        setValidationStatus('Upload both Hunt and Kills files to start validation.', 'secondary', false);
        importModal.show();
    });

    $('#rollbackBtn').click(function() {
        rollbackModal.show();
        loadRollbackReports();
    });

    $('#configSettingsBtn').click(async function() {
        if (!configSettingsModal) return;
        ensureConfigSkipSelector();
        configSettingsModal.show();
        await hydratePlayerSuggestions();
        await loadConfigSettings();
    });

    $('#saveConfigSettingsBtn').click(function() {
        saveConfigSettings();
    });

    $huntFile.on('change', validateSelectedFiles);
    $killsFile.on('change', validateSelectedFiles);
    $dateRange.on('apply.daterangepicker', function(ev, picker) {
        $(this).val(picker.startDate.format('YYYY/MM/DD') + ' - ' + picker.endDate.format('YYYY/MM/DD'));
        updateUniqueId();
    });
    $dateRange.on('cancel.daterangepicker', function() {
        $(this).val('');
        updateUniqueId();
    });

    // Submit import
    $('#submitImport').click(function() {
        if (isImportSubmitting) {
            return;
        }

        const hunt = $huntFile[0]?.files?.[0];
        const kills = $killsFile[0]?.files?.[0];
        const dateRange = $dateRange.val();
        const uniqueId = $uniqueId.val();

        if (!hunt || !kills) {
            showAlert('Upload both Hunt and Kills files.', 'danger');
            return;
        }

        if (!dateRange || !dateRange.includes(' - ')) {
            showAlert('Select date range before upload.', 'danger');
            return;
        }

        const [fromRaw, toRaw] = dateRange.split(' - ');
        const fromDate = moment(fromRaw, 'YYYY/MM/DD', true);
        const toDate = moment(toRaw, 'YYYY/MM/DD', true);
        if (!fromDate.isValid() || !toDate.isValid()) {
            showAlert('Date range is invalid.', 'danger');
            return;
        }

        const formData = new FormData();
        formData.append('huntFile', hunt);
        formData.append('killsFile', kills);
        formData.append('fromDate', fromDate.format('YYYY-MM-DD'));
        formData.append('toDate', toDate.format('YYYY-MM-DD'));
        formData.append('uniqueId', uniqueId);

        isImportSubmitting = true;
        $submitImport.prop('disabled', true);
        setButtonLoading('#submitImport', true, 'Uploading...', 'Upload Report');

        $.ajax({
            url: '/Home/ImportFromFiles',
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            success: function(response) {
                if (response.success) {
                    importModal.hide();
                    showAlert(response.message, 'success');
                    if (typeof window.refreshAvailableWeeks === 'function') {
                        Promise.resolve(window.refreshAvailableWeeks(response.uniqueIdentifier || null))
                            .then(() => {
                                if (typeof window.generateReport === 'function') {
                                    window.generateReport();
                                }
                            });
                    }
                } else {
                    setValidationStatus(response.message || 'Import failed.', 'danger', false);
                }
            },
            error: function() {
                setValidationStatus('Error occurred while importing uploaded files.', 'danger', false);
            },
            complete: function() {
                isImportSubmitting = false;
                setButtonLoading('#submitImport', false, '', 'Upload Report');
                setImportControlsEnabled(true);
            }
        });
    });

    $('#confirmDeleteReport').click(function() {
        const statsId = $rollbackDropdown.val();
        if (!statsId) {
            showAlert('Select a report to delete.', 'danger');
            return;
        }

        const userConfirmed = window.confirm('Are you sure you want to permanently delete this uploaded report?');
        if (!userConfirmed) {
            return;
        }

        setButtonLoading('#confirmDeleteReport', true, 'Deleting...', 'Delete');
        $.ajax({
            url: '/Report/DeleteUploadedReport',
            type: 'POST',
            data: { statsId: statsId },
            success: function(response) {
                showAlert(response.message, response.success ? 'success' : 'danger');
                if (response.success) {
                    rollbackModal.hide();
                    if (typeof window.refreshAvailableWeeks === 'function') {
                        window.refreshAvailableWeeks();
                    }
                }
            },
            error: function() {
                showAlert('Error occurred while deleting report.', 'danger');
            },
            complete: function() {
                setButtonLoading('#confirmDeleteReport', false, '', 'Delete');
            }
        });
    });

    // Backup button click
    $('#backupBtn').click(function() {
        confirmBackupModal.show();
    });

    // Confirm backup
    $('#confirmBackup').click(function() {
        setButtonLoading('#confirmBackup', true, 'Processing...', 'Confirm');

        $.ajax({
            url: '/Home/BackupToDrive',
            type: 'POST',
            success: function(response) {
                confirmBackupModal.hide();
                showAlert(response.message, response.success ? 'success' : 'danger');
            },
            error: function() {
                showAlert('Error occurred while backing up', 'danger');
            },
            complete: function() {
                setButtonLoading('#confirmBackup', false, '', 'Confirm');
            }
        });
    });

    // Sync button click
    $('#syncBtn').click(function() {
        confirmSyncModal.show();
    });

    // Confirm sync
    $('#confirmSync').click(function() {
        setButtonLoading('#confirmSync', true, 'Processing...', 'Confirm');

        $.ajax({
            url: '/Home/SyncFromDrive',
            type: 'POST',
            success: function(response) {
                confirmSyncModal.hide();
                showAlert(response.message, response.success ? 'success' : 'danger');
            },
            error: function() {
                showAlert('Error occurred while syncing', 'danger');
            },
            complete: function() {
                setButtonLoading('#confirmSync', false, '', 'Confirm');
            }
        });
    });
});

function setButtonLoading(selector, isLoading, loadingText, defaultText) {
    const btn = $(selector);
    if (isLoading) {
        btn.prop('disabled', true);
        btn.html(`<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>${loadingText}`);
    } else {
        btn.prop('disabled', false);
        btn.text(defaultText);
    }
}

function showAlert(message, type) {
    const alert = `<div class="alert alert-${type} alert-dismissible fade show" role="alert">
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    </div>`;
    
    $('#alerts').append(alert);
    
    // Auto dismiss after 5 seconds
    setTimeout(function() {
        document.querySelectorAll('#alerts .alert').forEach(a => bootstrap.Alert.getOrCreateInstance(a).close());
    }, 5000);
}