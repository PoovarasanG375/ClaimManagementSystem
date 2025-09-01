$(document).ready(function () {
    let currentCard = null;

    // View button opens modal
    $('.view-btn').on('click', function () {
        currentCard = $(this).closest('.claim-card');

        const claimId = $(this).data('claim-id');
        const policyName = $(this).data('policy-name');
        const policyholderName = $(this).data('policyholder-name');
        const claimAmount = $(this).data('claim-amount');
        const coverageAmount = $(this).data('coverage-amount');
        const submittedDate = $(this).data('submitted-date');
        const status = $(this).data('status');

        $('#claimDetails').html(`
            <p><strong>Claim ID:</strong> ${claimId}</p>
            <p><strong>Policy Name:</strong> ${policyName}</p>
            <p><strong>Policyholder:</strong> ${policyholderName}</p>
            <p><strong>Claim Amount:</strong> ₹${claimAmount}</p>
            <p><strong>Coverage Amount:</strong> ₹${coverageAmount}</p>
            <p><strong>Submitted Date:</strong> ${submittedDate}</p>
            <p><strong>Current Status:</strong> <span class="badge ${status === 'Pending' ? 'bg-warning text-dark' : status === 'Approved' ? 'bg-success' : 'bg-danger'}">${status}</span></p>
        `);

        $('#statusSelect').val(status);
        $('#claimModal').modal('show');
    });

    // Save status change
    $('#saveStatus').on('click', function () {
        const newStatus = $('#statusSelect').val();
        let badgeClass = 'bg-warning text-dark';
        if (newStatus === 'Approved') badgeClass = 'bg-success';
        else if (newStatus === 'Rejected') badgeClass = 'bg-danger';

        currentCard.find('.badge')
            .removeClass()
            .addClass('badge ' + badgeClass)
            .text(newStatus);

        $('#claimModal').modal('hide');

        // Reapply filter after status change
        const selected = $('.form-select').val().toLowerCase();
        $('.claim-card').each(function () {
            const status = $(this).find('.badge').text().toLowerCase();
            if (selected === 'all claims' || selected === 'all') {
                $(this).show();
            } else if (status === selected) {
                $(this).show();
            } else {
                $(this).hide();
            }
        });
    });

    // Filter cards by status
    $('.form-select').on('change', function () {
        const selected = $(this).val().toLowerCase();

        $('.claim-card').each(function () {
            const status = $(this).find('.badge').text().toLowerCase();

            if (selected === 'all claims' || selected === 'all') {
                $(this).show();
            } else if (status === selected) {
                $(this).show();
            } else {
                $(this).hide();
            }
        });
    });

    $('#claimModal').on('hidden.bs.modal', function () {
        if ($('.modal-backdrop').length) {
            $('.modal-backdrop').remove();
        }
        $('body').removeClass('modal-open');
    });
});
