var dataTable;

$(document).ready(function () {
    const urlParams = new URLSearchParams(window.location.search);
    const status = urlParams.get('status');
    loadDataTable(status);
});

function loadDataTable(status) {
    dataTable = $('#BookingTable').DataTable({
        "ajax": {
            url: '/Booking/GetAllBooking?status='+status
        },
        columns: [
            { data: 'id' },
            { data: 'name' },
            { data: 'phone' },
            { data: 'email' },
            { data: 'status' },
            { data: 'checkInDate' },
            { data: 'nights' },
            {
                data: 'totalCost',
                render: function (data) {
                    var formattedCost = new Intl.NumberFormat('en-AE', { style: 'currency', currency: 'AED' }).format(data);
                    return formattedCost;
                }
            },
            {
                data: 'id',
                "render": function (data) {
                    return `<div class="w-75 btn-group">
    <a href="/Booking/BookingDetails?bookingId=${data}" class="btn btn-outline-primary mx-2">
        <i class="bi bi-pencil-square"></i> View Booking Details
    </a>
                    </div>`
                }


            }
        ],
        "columnDefs": [
            { "width": "10%", "targets": [0, 1, 2, 3, 4] }
        ],
        "drawCallback": function () {
            $('.spinner').hide();
        }
    });
}
