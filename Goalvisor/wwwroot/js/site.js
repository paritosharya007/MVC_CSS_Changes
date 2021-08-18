var siteDateFotmat = "YYYY-MM-DDTHH:mm:ss";

function notifySucess(message) {
    $.notify({
        message: message
    }, {
        type: 'success',
        z_index: 50000,
        offset: { x: 20, y: 60 },
    });
}

function notifyError(message) {
    $.notify({
        message: message
    }, {
        type: 'danger',
        z_index: 50000,
        offset: { x: 20, y: 60 }
    });
}

function deserializeIsoDate(stringDate) {
    return moment(stringDate, siteDateFotmat);
}

function serializeIsoDate(dateToSerialize) {
    return moment().toISOString().substring(0, 18);
}

function renderSimpleDate(stringDate) {
    let momentDate = deserializeIsoDate(stringDate);
    let strMonth = momentDate.month();
    strMonth = strMonth + 1;
    if (strMonth < 10) {
        strMonth = "0" + strMonth.toString();
    }

    let strDay = momentDate.date();
    if (strDay < 10) {
        strDay = "0" + strDay.toString();
    }
    return momentDate.year() + "-" + strMonth + "-" + strDay;
}

var phoneRegEx = RegExp("^[+]*[(]{0,1}[0-9]{1,4}[)]{0,1}[-\s\./0-9]*$")