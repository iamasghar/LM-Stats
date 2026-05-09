$(document).ready(function() {
    // Initialize date range picker
    $('.daterange').daterangepicker({
        opens: 'left',
        autoUpdateInput: false,
        locale: {
            format: 'YYYY/MM/DD',
            cancelLabel: 'Clear'
        }
    });

    const importModal = new bootstrap.Modal(document.getElementById('importModal'));
    const confirmBackupModal = new bootstrap.Modal(document.getElementById('confirmBackupModal'));
    const confirmSyncModal = new bootstrap.Modal(document.getElementById('confirmSyncModal'));

    $('.daterange').on('apply.daterangepicker', function(ev, picker) {
        $(this).val(picker.startDate.format('YYYY/MM/DD') + ' - ' + picker.endDate.format('YYYY/MM/DD'));
        // Generate unique ID
        const uniqueId = `${picker.startDate.format('YYYYMMDD')}_${picker.endDate.format('YYYYMMDD')}_${new Date().getTime()}`;
        $('#uniqueId').val(uniqueId);
    });

    $('.daterange').on('cancel.daterangepicker', function(ev, picker) {
        $(this).val('');
        $('#uniqueId').val('');
    });

    // Import button click
    $('#importBtn').click(function() {
        importModal.show();
    });

    // Submit import
    $('#submitImport').click(function() {
        const dateRange = $('#dateRange').val();
        if (!dateRange) {
            showAlert('Please select a date range', 'danger');
            return;
        }

        const dates = dateRange.split(' - ');
        const fromDate = dates[0];
        const toDate = dates[1];
        const uniqueId = $('#uniqueId').val();

        setButtonLoading('#submitImport', true, 'Submitting...', 'Submit');

        $.ajax({
            url: '/Home/ImportFromSheets',
            type: 'POST',
            data: {
                fromDate: fromDate,
                toDate: toDate,
                uniqueId: uniqueId
            },
            success: function(response) {
                importModal.hide();
                showAlert(response.message, response.success ? 'success' : 'danger');
            },
            error: function() {
                showAlert('Error occurred while importing Excel data', 'danger');
            },
            complete: function() {
                setButtonLoading('#submitImport', false, '', 'Submit');
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