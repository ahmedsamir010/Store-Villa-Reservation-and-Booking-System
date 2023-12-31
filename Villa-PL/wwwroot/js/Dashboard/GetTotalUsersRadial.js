$(document).ready(function () {
    loadTotalUserCharts();
});

function loadTotalUserCharts() {
    $(".chart-spinner").show();

    $.ajax({
        url: "/Dashboard/GetTotalUsersRadialCharts",
        type: 'GET',
        dataType: 'json',
        success: function (data) {
            document.querySelector("#spanTotalUserCount").innerHTML = data.totalCount;
            var sectionCurrentCount = document.createElement("span");
            if (data.hasRatioIncreased) {
                    sectionCurrentCount.className = "text-success me-1";
                    sectionCurrentCount.innerHTML = '<i class="bi bi-arrow-up-right-circle me-1"</i> <span>' + data.countInCurrentMonth
            }

            else {
                sectionCurrentCount.className = "text-danger me-1";
                sectionCurrentCount.innerHTML = '<i class="bi bi-arrow-down-right-circle me-1"</i> <span>' + data.countInCurrentMonth
            }

            document.querySelector("#sectionUserCount").append(sectionCurrentCount);
            document.querySelector("#sectionUserCount").append('Sice Last  Month');

            loadRadialBarCharts("totalUserRadialChart", data);
            $(".chart-spinner").hide();
        }
    })
}

