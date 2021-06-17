//Admission Date
function admissionDate(context) {
    var saveEvent = context.getEventArgs();
    var admissionDate = Xrm.Page.getAttribute("mdoc_admissiondate").getValue();
    var DischargeDate = Xrm.Page.getAttribute("mdoc_dischargedate").getValue();
    var transferDate = Xrm.Page.getAttribute("mdoc_transferdate").getValue();
    if (admissionDate != null) {

        if (DischargeDate != null) {
            if (DischargeDate.setHours(0, 0, 0, 0) < admissionDate.setHours(0, 0, 0, 0)) {
                alert("Discharge Date cannot be earlier than the Admission Date");
                Xrm.Page.getAttribute("mdoc_admissiondate").setValue(null);
                saveEvent.preventDefault();
            }
        }
        if (transferDate != null) {
            if (transferDate.setHours(0, 0, 0, 0) < admissionDate.setHours(0, 0, 0, 0)) {
                alert("Transfer Date cannot be earlier than the Admission Date");
                Xrm.Page.getAttribute("mdoc_admissiondate").setValue(null);
                saveEvent.preventDefault();
            }
        }

    }
}