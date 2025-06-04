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
        $('#importModal').modal('show');
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

        $.ajax({
            url: '/Home/ImportFromSheets',
            type: 'POST',
            data: {
                fromDate: fromDate,
                toDate: toDate,
                uniqueId: uniqueId
            },
            success: function(response) {
                $('#importModal').modal('hide');
                showAlert(response.message, response.success ? 'success' : 'danger');
            },
            error: function() {
                showAlert('Error occurred while importing data', 'danger');
            }
        });
    });

    // Backup button click
    $('#backupBtn').click(function() {
        $('#confirmBackupModal').modal('show');
    });

    // Confirm backup
    $('#confirmBackup').click(function() {
        $.ajax({
            url: '/Home/BackupToDrive',
            type: 'POST',
            success: function(response) {
                $('#confirmBackupModal').modal('hide');
                showAlert(response.message, response.success ? 'success' : 'danger');
            },
            error: function() {
                showAlert('Error occurred while backing up', 'danger');
            }
        });
    });

    // Sync button click
    $('#syncBtn').click(function() {
        $('#confirmSyncModal').modal('show');
    });

    // Confirm sync
    $('#confirmSync').click(function() {
        $.ajax({
            url: '/Home/SyncFromDrive',
            type: 'POST',
            success: function(response) {
                $('#confirmSyncModal').modal('hide');
                showAlert(response.message, response.success ? 'success' : 'danger');
            },
            error: function() {
                showAlert('Error occurred while syncing', 'danger');
            }
        });
    });
});

function showAlert(message, type) {
    const alert = `<div class="alert alert-${type} alert-dismissible fade show" role="alert">
        ${message}
        <button type="button" class="close" data-dismiss="alert" aria-label="Close">
            <span aria-hidden="true">&times;</span>
        </button>
    </div>`;
    
    $('#alerts').append(alert);
    
    // Auto dismiss after 5 seconds
    setTimeout(function() {
        $('.alert').alert('close');
    }, 5000);
}