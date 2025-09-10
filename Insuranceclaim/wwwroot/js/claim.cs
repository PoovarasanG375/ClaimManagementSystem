$(document).ready(function() {
 
    // Show/hide claim cards based on the selected filter option
 
    $('select[data-filter-select]').on('change', function() {

        const selectedStatus = $(this).val();
 
        $('.claim-card').each(function() {

            const cardStatus = $(this).data('claim-status');

            if (selectedStatus === 'All' || cardStatus === selectedStatus)
            {
 
                $(this).show();

            }
            else
            {
 
                $(this).hide();

            }

        });

    });
 
    // Handle view button click to populate modal
 
    $('.view-btn').on('click', function() {

        const card = $(this).closest('.claim-card');

        const claimData = {

            claimId: card.data('claim-id'),
 
            claimAmount: card.data('claim-amount'),
 
            coverageAmount: card.data('coverage-amount'),
 
            claimDate: card.data('claim-date'),
 
            claimStatus: card.data('claim-status'),
 
            policyName: card.data('policy-name'),
 
            policyholderName: card.data('policyholder-name'),
 
            adjusterName: card.data('adjuster-name'),
 
            descriptionOfIncident: card.data('description-of-incident'),
 
            adjusterNotes: card.data('adjuster-notes'),
 
            adminNotes: card.data('admin-notes'),
 
        }
    ;
 
        $('#modal-claim-id').text(claimData.claimId);
 
        $('#modal-policyholder-name').text(claimData.policyholderName || 'N/A');
 
        $('#modal-policy-name').text(claimData.policyName || 'N/A');
 
        $('#modal-claim-amount').text(claimData.claimAmount ? '₹' + parseFloat(claimData.claimAmount).toLocaleString('en-IN') : 'N/A');
 
        $('#modal-coverage-amount').text(claimData.coverageAmount ? '₹' + parseFloat(claimData.coverageAmount).toLocaleString('en-IN') : 'N/A');
 
        $('#modal-submitted-date').text(claimData.claimDate || 'N/A');
 
        $('#modal-adjuster-name').text(claimData.adjusterName || 'N/A');
 
        $('#modal-description-of-incident').text(claimData.descriptionOfIncident || 'N/A');
 
        $('#modal-adjuster-notes').text(claimData.adjusterNotes || 'N/A');
 
        $('#modal-current-status').text(claimData.claimStatus || 'N/A');
 
        // Populate the form fields inside the modal
 
        $('#adminNotesTextarea').val(claimData.adminNotes);
 
        $('#statusSelect').val(claimData.claimStatus);
 
        // Store the claimId on the save button
 
        $('#saveStatus').data('claimId', claimData.claimId);

});
 
    // Handle save button click
 
    $('button[data-action="save-status"]').on('click', function() {

    const claimId = $(this).data('claimId');

    const newStatus = $('#statusSelect').val();

    const adminNotes = $('#adminNotesTextarea').val();

    const antiForgeryToken = $('input[name="__RequestVerificationToken"]').val();
 
        $.ajax({

    url: '/Admin/AdminClaims/UpdateClaimDetails',
 
            type: 'POST',
 
            contentType: 'application/json',
 
            headers:
        {

            'RequestVerificationToken': antiForgeryToken


            },
 
            data: JSON.stringify({

        ClaimId: claimId,
 
                Status: newStatus,
 
                AdminNotes: adminNotes


            }),
 
            success: function(response) {

            if (response.success)
            {

                alert('Status updated successfully!');

                location.reload();

            }
            else
            {

                alert('Error: ' + response.message);

            }

        },
 
            error: function(xhr, status, error) {

            alert('An error occurred while updating the claim.');

            console.error(xhr.responseText);

        }

    });

});
 
});


claim.js
 