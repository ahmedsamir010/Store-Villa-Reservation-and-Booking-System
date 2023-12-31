$(document).ready(function () {
    loadCustomerPieCharts();
});

function loadCustomerPieCharts() {
    $(".chart-spinner").show();

    $.ajax({
        url: "/Dashboard/GetBookingPieCharts",
        type: 'GET',
        dataType: 'json',
        success: function (data) {
            loadPieCharts("customerBookingsPieChart", data);
            console.log("Data:", data);

            $(".chart-spinner").hide();
        }
    });
}

function loadPieCharts(id, data) {
    var chartColors = getChartColorsArray(id);
    var options = {
        colors: chartColors,
        series: data.series,
        labels: data.label,
        chart: {
            width: 380,
            type: 'pie',
        },
        stroke: {
            show: false
        },
        legend: {
            position: 'bottom',
            horizontalAlign: 'center',
            labels: {
                colors: "#fff",
                useSeriesColors: true
            },
        },
    };
    var chart = new ApexCharts(document.querySelector("#" + id), options);
    chart.render();
}
