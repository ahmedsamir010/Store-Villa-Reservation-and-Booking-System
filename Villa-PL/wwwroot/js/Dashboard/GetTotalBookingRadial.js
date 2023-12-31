$(document).ready(function () {
    loadTotalBookingCharts();
});

function loadTotalBookingCharts() {
    $(".chart-spinner").show();

    $.ajax({
        url: "/Dashboard/GetTotalBookingRadialCharts",
        type: 'GET',
        dataType: 'json',
        success: function (data) {
            document.querySelector("#spanTotalBookingCount").innerHTML = data.totalCount;
            var sectionCurrentCount = document.createElement("span");
            if (data.hasRatioIncreased) {
                    sectionCurrentCount.className = "text-success me-1";
                    sectionCurrentCount.innerHTML = '<i class="bi bi-arrow-up-right-circle me-1"</i> <span>' + data.countInCurrentMonth
            }

            else {
                sectionCurrentCount.className = "text-danger me-1";
                sectionCurrentCount.innerHTML = '<i class="bi bi-arrow-down-right-circle me-1"</i> <span>' + data.countInCurrentMonth
            }

            document.querySelector("#sectionBookingCount").append(sectionCurrentCount);
            document.querySelector("#sectionBookingCount").append('Sice Last  Month');

            loadRadialBarCharts("totalBookingsRadialChart", data);
            $(".chart-spinner").hide();
        }
    })
}

