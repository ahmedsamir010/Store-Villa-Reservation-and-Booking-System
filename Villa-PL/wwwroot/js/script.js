function LoadVillaList() {
    var objData = {
        checkInDate: $("#CheckInDate").val(),
        nights: $("#Nights").val()
    };

    $.ajax({
        url: "@Url.Action("GetVillaListByDate", "Home")",
        data: objData,
        type: "POST",
        success: function (data) {
            // Use the # symbol to select the element by ID
            $("#VillaList").empty();
            $("#VillaList").html(data);
        },
    });
}
