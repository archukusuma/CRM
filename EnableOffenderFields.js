//enableOffenderFields

function enableFields() {
    var roles = Xrm.Page.context.getUserRoles();
    var hasRole = false;
    var hasTCRole = false;

    for (var i = 0; i < roles.length; i++) {
        GetRole(roles[i]);

        if (roleName.trim() == "Timecomp - Update") {
            hasTCRole = true;
            break;
        }

        if (roleName.trim() == "ADSS Administrator") {
            hasRole = true;
        }
    }

    if (hasTCRole) {
        Xrm.Page.getAttribute("mdoc_controllingpbjsentenceid").controls.get(0).setDisabled(false);
        Xrm.Page.getAttribute("mdoc_controllingminimumsentenceid").controls.get(0).setDisabled(false);
        Xrm.Page.getAttribute("mdoc_controllingmaximumsentenceid").controls.get(0).setDisabled(false);
    }
    else {
        Xrm.Page.getAttribute("mdoc_controllingpbjsentenceid").controls.get(0).setDisabled(true);
        Xrm.Page.getAttribute("mdoc_controllingminimumsentenceid").controls.get(0).setDisabled(true);
        Xrm.Page.getAttribute("mdoc_controllingmaximumsentenceid").controls.get(0).setDisabled(true);
    }
    if (hasTCRole || hasRole) {
        Xrm.Page.getAttribute("mdoc_minimumdate").controls.get(0).setDisabled(false);
        Xrm.Page.getAttribute("mdoc_maximumdate").controls.get(0).setDisabled(false);
        Xrm.Page.getAttribute("mdoc_paroleboardjurisdictiondate").controls.get(0).setDisabled(false);
    }
    else {
        Xrm.Page.getAttribute("mdoc_minimumdate").controls.get(0).setDisabled(true);
        Xrm.Page.getAttribute("mdoc_maximumdate").controls.get(0).setDisabled(true);
        Xrm.Page.getAttribute("mdoc_paroleboardjurisdictiondate").controls.get(0).setDisabled(true);
    }
}

function GetRole(roleid) {
    var serverUrl = Xrm.Page.context.getClientUrl();
    var oDataSelect = serverUrl + "/XRMServices/2011/OrganizationData.svc/RoleSet?$select=Name&$filter=RoleId eq guid'" + roleid + "'";

    var retrieveReq = new XMLHttpRequest();
    retrieveReq.open("GET", oDataSelect, false);
    retrieveReq.setRequestHeader("Accept", "application/json");
    retrieveReq.setRequestHeader("Content-Type", "application/json;charset=utf-8");
    retrieveReq.onreadystatechange = function () {
        GetRoleData(this);
    };
    retrieveReq.send();
}

function GetRoleData(retrieveReq) {
    if (retrieveReq.readyState == 4) {
        if (retrieveReq.status == 200 || retrieveReq.status == 201) {
            var retrieved = JSON.parse(retrieveReq.responseText).d;
            roleName = retrieved.results[0].Name;
        }
    }
}


//The below script is used for only time computation and Jos processing forms
function enableFieldsJOS() {

    var roles = Xrm.Page.context.getUserRoles();
    var hasTCRole = false;



    for (var i = 0; i < roles.length; i++) {

        GetRole(roles[i]);



        if (roleName.trim() == "Timecomp - Update") {

            hasTCRole = true;

            break;

        }

    }



    if (hasTCRole) {

        Xrm.Page.getAttribute("mdoc_controllingpbjsentenceid").controls.get(0).setDisabled(false);

        Xrm.Page.getAttribute("mdoc_controllingminimumsentenceid").controls.get(0).setDisabled(false);

        Xrm.Page.getAttribute("mdoc_controllingmaximumsentenceid").controls.get(0).setDisabled(false);

    }

    else {

        Xrm.Page.getAttribute("mdoc_controllingpbjsentenceid").controls.get(0).setDisabled(true);

        Xrm.Page.getAttribute("mdoc_controllingminimumsentenceid").controls.get(0).setDisabled(true);

        Xrm.Page.getAttribute("mdoc_controllingmaximumsentenceid").controls.get(0).setDisabled(true);

    }
}
