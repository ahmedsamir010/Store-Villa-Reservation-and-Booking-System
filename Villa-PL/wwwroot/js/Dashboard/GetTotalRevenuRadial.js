$(document).ready(function () {
    loadTotalRevenueCharts();
});

function loadTotalRevenueCharts() {
    $(".chart-spinner").show();

    $.ajax({
        url: "/Dashboard/GetTotalRevenueRadialCharts",
        type: 'GET',
        dataType: 'json',
        success: function (data) {
            var h4Element = document.getElementById("totalRevenu");
            var totalCount = document.getElementById("totalRevenuCount");
            var sectionCurrentCount = document.createElement("span");

            if (data.hasRatioIncreased) {
                sectionCurrentCount.className = "text-success me-1";
                sectionCurrentCount.innerHTML = '<i class="bi bi-arrow-up-right-circle me-1"></i>' + data.countInCurrentMonth;
            } else {
                sectionCurrentCount.className = "text-danger me-1";
                sectionCurrentCount.innerHTML = '<i class="bi bi-arrow-down-right-circle me-1"></i>' + data.countInCurrentMonth;
            }

            totalCount.appendChild(sectionCurrentCount);
            totalCount.innerHTML += ' Since Last Month';

            h4Element.innerHTML = data.countInCurrentMonth;

            loadRadialBarCharts("totalRevenueRadialChart", data);
            $(".chart-spinner").hide();
        }
    });
}
