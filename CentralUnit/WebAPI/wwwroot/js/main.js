
let inField = [];
let  startQue = [];
let armedDNF = [];
let stoppedRiders = [];
let armedDNS = [];

var stopEventActive = false;
var riderInStartPosition = false;
var goButtonHidden = false;
var waitingId;
var waitingIndex;
var showAll = false;
var firstUpButtonShown = false;
var ignoreDNFrule = false;
var row;
var probablyStopped;
var lastStoppedRider;
var ignoredStopEvent = false;
var delayTime = 15000;  // For testing set to low value. Live should be a larger value (around 20000).

var forTestingOnly;

function uuidv4() {
    // create a unique id
  return ([1e7]+-1e3+-4e3+-8e3+-1e11).replace(/[018]/g, c =>
    (c ^ crypto.getRandomValues(new Uint8Array(1))[0] & 15 >> c / 4).toString(16)
  );
}

// test ajax request
const xhttp = new XMLHttpRequest();
	xhttp.onload = function() {
    	alert(this.responseText);
    }
xhttp.open("GET", "/RaceTracking/State", true);
xhttp.send()

// Some random data to test with:

inField.push({nr: 9, id: uuidv4(), name: "Paul Hille", time: 30, p1: 0, p3: 0, dnf: false, dsq: false});

inField.push({nr: 18, id: uuidv4(), name: "Richard van Schouwenburg", time: 145, p1: 0, p3: 0, dnf: false, dsq: false});

startQue.push({nr: 7, id: uuidv4(), name: "Martijn Stapelbroek"});
startQue.push({nr: 11, id: uuidv4(), name: "Romke Roodbaard"});
startQue.push({nr: 5, id: uuidv4(), name: "Bert Schuld"});
startQue.push({nr: 45, id: uuidv4(), name: "Marie Geerlings"});
startQue.push({nr: 6, id: uuidv4(), name: "Test Rider"});
startQue.push({nr: 8, id: uuidv4(), name: "Test Rider"});
startQue.push({nr: 14, id: uuidv4(), name: "Test Rider"});

showInField();
showStartQue();



function riderDNF (filteredDNFelement) {
    // Send id to API
    console.log("Set rider as DNF: ", filteredDNFelement.id);
    //change 'sent' variable to 'true' to avoid sending same information to API more then once.
    armedDNF.forEach((armedDNFelement, index) => {if (armedDNFelement.id === filteredDNFelement.id) {armedDNF[index].sent = true} });
    
    
    
    
    
    
    
    
    /*
     * 
     * 
     * 
     *      FOR TESTING ONLY!
     *      REMOVE CODE LATER
     * 
     * 
     * 
    */



    inField = inField.filter(element => element.id != filteredDNFelement.id);



    /*
     * 
     * 
     * 
     *          END OF 'FOR TESTING ONLY'
     * 
     * 
     * 
     */



    
    
    
    
    
    
    
    
    
    
    
    
    
    // delete the element from armedDNF.
    armedDNF = armedDNF.filter(armedDNFelement => filteredDNFelement.id != armedDNFelement.id);
    showInField();
}


function dnfCheck() {
    // When selected, a rider gets a DNF status within UI. These riders are stored in armedDNF array.
    // After a delay of xxx milliseconds, the DNF will be communicated to API. And deleted from armedDNF array.
    
    // Search for DNF riders who are there for more then xxx milliseconds:
    const filteredDNF = armedDNF.filter(element => element.time < Date.now()-delayTime && element.sent === false);
    

    // Communicate to API:
    filteredDNF.forEach(riderDNF);
}


function stoppedCheck() {
    stoppedRiders = stoppedRiders.filter(element => element.stopTime > Date.now() - delayTime);   // Set to 5000 for TESTING purposes. Should be longer.
    showInField();
}




function showInField() {
    row = 0; // Resets rownumber
    document.getElementById('inField').innerHTML = "";  // Empty the table first
    showStartPosition();    // Shows the rider who is in the start box
    
    
    inField = sortArrayWithObjects(inField, "time");
    inField.forEach(addRowInField);     // Add all the riders row by row to the HTML table  who are now riding (in the field)
    
    
        
    // At the bottom of inField table, show the stopped riders and riders with a DNF flag:
    
    // Combine info from dnfRiders and stoppedRiders:
    let vanishingRiders = [];
    armedDNF.forEach(       element => vanishingRiders.push({id: element.id, time: element.time,                type: "dnf"}) );
    stoppedRiders.forEach( element => vanishingRiders.push({id: element.id, time: element.stopTime,    type: "stopped"}) );
    
    // Sort the array to display all the riders in order of time:
    vanishingRiders = sortArrayWithObjects(vanishingRiders, "time", "dsc");
    
    // Now display:
    vanishingRiders.forEach(vanishingElement => 
        {
            if (vanishingElement.type === "stopped") {
                let filtered = stoppedRiders.filter(stoppedRidersElement => stoppedRidersElement.id === vanishingElement.id);
                addRowInField(filtered[0]);
            }
            else if (vanishingElement.type === "dnf") {
                // DNF riders are still 'in the field'. Therefore, their info must be retreived from 'inField'.
                let filtered = inField.filter(inFieldElement => inFieldElement.id === vanishingElement.id);
                ignoreDNFrule = true;   // Otherwise the DNF rider cannot be displayed (see addRowInField()).
                addRowInField(filtered[0]);
                ignoreDNFrule = false;  // Reset variable.
            }
        }
    );
    
    
   
    // Create the IGNORE button:
    document.getElementById("inField").innerHTML +=
    '<tr><td colspan="15" style="height: 20px;"></td><td class="btn1 ignore' + hideIgnoreButton() + '" onpointerdown="ignoreStopEvent()">' + ignoreButtonText() + '</td></tr>';
}


var ignoreStopEventTimeout;

function ignoreStopEvent() {
    // The last stop event was not a valid stop event and should be ignored.
    
    
    // If 'ignoredStopEvent' is true, than we should cancel last action.
    if (ignoredStopEvent) {
        stopEvent();
        console.log("Last stop event should no longer be ignored.");
        showInField();
        return;
    }
    
    
    stopEventActive = false;
    clearInterval(flashingInterval);
    flashingInterval = null;
    
    ignoredStopEvent = true;
    ignoreStopEventTimeout = setTimeout(() => ignoredStopEvent = false, delayTime);
    
    console.log("Ignore Stop Event.");
    showInField();
}

function showStartQue() {
    row = 0; // Resets row number
    
    // Display all the rider who still have to start.
    
    var el = document.getElementById('startQue');
    
    firstUpButtonShown = false; // Hide the first 'up' button
    
    el.innerHTML = "";  // Empty the HTML table
    startQue.forEach(addRowStartQue);   // Add all the riders row by row to the table.
    
    
    // Create the "SHOW ALL" function
    if (!showAll) {
        el.innerHTML +=
        '<tr><td colspan="2"></td><td colspan="7" class="btn1 showAll" onClick="showCompleteStartQue()">&#8595; SHOW ALL</td></tr>';
    }
    else {
        el.innerHTML +=
        '<tr><td colspan="2"></td><td colspan="7" class="btn1 showAll" onClick="makeStartQueSmaller()">&#8593; SHOW LESS</td></tr>';  
    }
}


function showCompleteStartQue() {
    // Shows all the riders in the start que
    showAll = true;
    showStartQue();
}

function makeStartQueSmaller() {
    // Collapses the start que to compact view
    showAll = false;
    showStartQue();
}



function showStartPosition() {
    let index;
    
    // Displays the rider in the startbox and buttons to control start light.
    
    if (!riderInStartPosition) { // Checks to see if there is a rider waiting
        return;
    }
    
    if (isRiderStarted()) { // After rider appears in the Field (information from API, rider will be removed from startBox
        return;
    }
    
    const filteredStartQue = startQue.filter(element => element.id === waitingId);
//    console.log(filteredStartQue);
    const riderInfo = filteredStartQue[0];
//    console.log(riderInfo);
    
    // There must be a rider in start position, so below code will be executed:
    
    document.getElementById('inField').innerHTML += 
            '<tr class="row">' +
                '<td id="startBoxNr" class="tbCell riderNr">' +
                    riderInfo.nr +
                '</td>' +
                '<td id="startBoxName' + index +'" class="tbCell riderName startBoxName">' +
                    riderInfo.name +
                '</td>' +
                '<td id="greenLight" class="tbCell turnLightGreen btn'+ hideGoButton() +'" colspan=3 onClick="turnOnGreenLight(\'' + riderInfo.id + '\')">GO!</td>' +
                '<td id="cancelGreenLight" class="tbCell btn cancelGreenLight" colspan=3  onClick="cancelStart()">CANCEL</td>' +
                '<td colspan=8></td>' +                
              '</tr>';
}


function hideGoButton() {
    if (goButtonHidden) {
        return " hideThis";
    }
    else {
        return "";
    }
}


function turnOnGreenLight(id) {
    console.log("Turn on green light for: " + id);  // Send ID to API
    
    goButtonHidden = true;
    
    
    
    
    
    
    
    
    
    
    
    /*
     * 
     * 
     * 
     * 
     * 
     * 
     *  FOR TESTING ONLY
     * 
     * 
     * 
     * 
     * 
     * 
     * 
     * 
     * 
     * 
     */
    
    
    
            const filteredStartQue = startQue.filter(element => element.id === id);
            const riderObject = filteredStartQue[0];
            forTestingOnly = setTimeout(() => {inField.push({nr: riderObject.nr, id: id, name: riderObject.name, time: 1, p1: 0, p3: 0, dnf: false, dsq: false})}, 3000);
    
    
    
    
    /*
     * 
     * 
     * 
     * 
     * 
     * 
     * 
     * 
     *          END OF FOR TESTING ONLY
     * 
     * 
     * 
     * 
     * 
     * 
     * 
     * 
     * 
     * 
     * 
     */
    
    
    
    
    
    showInField();
    showStartQue();
}

function cancelStart() {    // Removes rider from start Box
    riderInStartPosition = false;
    goButtonHidden = false;
    console.log("Clear Start Box");
    
    clearTimeout(forTestingOnly);
    //forTestingOnly = null;
    
    showInField();
    showStartQue();
}


function addRowStartQue(currentValue, index, arr) {
    // If the rider is in start position, don't display it here.
    if (waitingId === currentValue['id'] && riderInStartPosition === true) {
        return;
    }
    
    if (index > 3 + riderInStartPosition && showAll === false) {
        return; // Don't show more then 4 items for the compact view
    }
    
    document.getElementById('startQue').innerHTML += 
          '<tr class="row">' +
                '<td id="startQueNr' + row +'" class="tbCell riderNr">' +
                    currentValue.nr +
                '</td>' +
                '<td id="startQueName' + row +'" class="tbCell riderName">' +
                    currentValue.name +
                '</td>' +

                '<td id="up' + row +'" class="tbCell btn1 positionChange' + hideFirstUpButton(index) + '" onClick="moveUpOrder(\'' + currentValue.id + '\', ' + row + ')")">' +
                    '&#8743;' +
                '</td>' +
                
                '<td class="empty">' +
                    
                '</td>' +

                '<td id="down' + row +'" class="tbCell btn1 positionChange' + hideLastDownButton(index) + '" onClick="moveDownOrder(\'' + currentValue.id + '\', ' + row + ')")">' +
                    '&#8744;' +
                '</td>' +
                
                '<td class="empty">' +
                    
                '</td>' +
               

                '<td id="startQueStart' + row +'" class="tbCell btn1 startButton' + hideGoButton() + '" onClick="sendRiderToStart(\'' + currentValue.id + '\', ' + row + ')")">' +
                    'START' +
                '</td>' +
                
                '<td class="empty">' +
                    
                '</td>' +

                '<td id="startQueDNS' + row +'" class="tbCell btn1 buttonDNS' + hideDNSbutton(currentValue.id) + '" onpointerdown="armDNS(\'' + currentValue.id + '\', ' + row + ')">' +
                    dnsButtonText(currentValue.id) +
                '</td>' +
            '</tr>';
    
    row++;
}


var isDNF = false;



function addRowInField(currentValue, index, arr) {
    
    // Dont't show a rider if he has a DNF status. These riders will be shown last. To make this possible, 'ignoreDNFrule' can be switched to 'true'.
    
    if (armedDNF.find(element => element.id == currentValue.id) && !ignoreDNFrule) {
        return;
    }
    
    document.getElementById('inField').innerHTML += 
            '<tr class="row">' +
                '<td id="inFieldNr' + row +'" class="tbCell riderNr">' +
                    currentValue.nr +
                '</td>' +
                '<td id="inFieldName' + row +'" class="tbCell riderName">' +
                    currentValue.name +
                '</td>' +
                '<td id="inFieldTime' + row +'" class="tbCell timer">' +
                    displayTime(currentValue.time) +
                '</td>' +

                '<td id="inFieldPlusS' + row +'" class="tbCell btn smallPenalty" onpointerdown="addSmallPenalty(\'' + currentValue.id + '\', ' + row + ')">' +
                    '+1s' +
                '</td>' + 
                
                '<td id="inFieldS" class="tbCell penaltyCounter">' +
                    currentValue.p1 +
                '</td>' +

                '<td id="inFieldMinS' + row +'" class="tbCell btn smallPenalty" onpointerdown="minusSmallPenalty(\'' + currentValue.id + '\', ' + row + ')">' +
                    "-1s" +
                '</td>' +

                '<td class="empty tbCell">' +

                '</td>' +

                '<td id="inFieldPlusB' + row +'" class="tbCell btn bigPenalty" onpointerdown="addBigPenalty(\'' + currentValue.id + '\', ' + row + ')">' +
                    "+3s" +
                '</td>' +
                
                '<td id="inFieldB" class="tbCell penaltyCounter">' +
                    currentValue.p3 +
                '</td>' +

                '<td id="inFieldMinB' + row +'" class="tbCell btn bigPenalty" onpointerdown="minusBigPenalty(\'' + currentValue.id + '\', ' + row + ')">' +
                    "-3s" +
                '</td>' +

                '<td class="empty tbCell">' +

                '</td>' +

                '<td id="inFieldDNF' + row +'" class="tbCell btn' + hideDNFbutton(currentValue.id) + ' dnf' + getDNFstatus(currentValue.id) + '" onpointerdown="armDNF(\'' + currentValue.id + '\', ' + row + ')">' +
                    getDNFbuttonText(currentValue.id) +
                '</td>' +

                '<td class="empty tbCell">' +

                '</td>' +

                '<td id="inFieldDSQ' + row +'" class="tbCell btn dsq' + isDSQ(currentValue.dsq) + '" onpointerdown="flagDSQ(\'' + currentValue.id + '\', ' + row + ')">' +
                    getDSQbuttonText(currentValue.dsq) +
                '</td>' +

                '<td class="empty tbCell">' +

                '</td>' +

                '<td id="inFieldStop' + row +'" class="tbCell ' + stopClass(currentValue.id) + ' btn' + checkStopEvent(currentValue.id) + '" onpointerdown="confirmStop(\'' + currentValue.id + '\', ' + row + ')">' +
                    stopButtonText(currentValue.id) +
                '</td>' + 
             '</tr>';
     
     
     row++; // Keep track of the row number.
}


function getDNFstatus (id) {
    if (armedDNF.find(element => element.id == id)) {
        return " selected";
    }
    else {
        return "";
    }
}



function getDSQbuttonText(dsq) {
    if (dsq) {
        return "cncl";
    }
    else {
        return "DSQ";
    }
}


function isDSQ(dsq) {
    if (dsq) {
        return " selected";
    }
    else {
        return "";
    }
}



function getDNFbuttonText(id) {
    if (!armedDNF.find(element => element.id == id)) {
        return "DNF";
    }
    else {
        return "cncl";
    }
}



function hideFirstUpButton(index) {
    if (index === 0 || !firstUpButtonShown) {
        firstUpButtonShown = true;
        return " hideThis";
    }
    else {
        return "";
    }
}


function hideLastDownButton(index) {
    var n = startQue.length;
    
    if (index === n-1) {
        return " hideThis";
    }
    
    return "";
}



var riderIsStarted;

function isRiderStarted() {
    // This function checks if the rider waiting to start is now in field.
    riderIsStarted = false;
    inField.forEach(    (currentValue, index) => { if (currentValue['id'] == waitingId) {
        riderIsStarted = true;
    }
        } );
    if (riderIsStarted) {
        // If the rider is found in the field (information from API), it can be removed from the start Box
        riderInStartPosition = false;
        
        
        
        
        
        
        
        
        
        
        
        /*
         * 
         * 
         * 
         * 
         * 
         * 
         *              FOR TESTING ONLY
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         */
        
        
        
        
                startQue = startQue.filter(element => element.id !== waitingId);
        
        
        
        
        /*
         * 
         * 
         *
         * 
         * 
         * 
         * 
         * 
         *          END OF FOR TESTING ONLY
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         */
        
        
        
        
        
        
        
        
        
        waitingId = "";
        goButtonHidden = false;
        
        
        
        
        
        
        
        
        
    /*
         * 
         * 
         * 
         * 
         * 
         * 
         *              FOR TESTING ONLY
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         */
        
        
        
        
                showStartQue();
        
        
        
        
        /*
         * 
         * 
         *
         * 
         * 
         * 
         * 
         * 
         *          END OF FOR TESTING ONLY
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         */
        
        
        
        
        
        
        
        return true;
    }
    else {
        return false;
    }
}


function checkStopEvent(riderId) {
    let riderIsStopped;
    const filtered = stoppedRiders.filter(element => element.id === riderId);
    if (filtered.length > 0) {
        riderIsStopped = true;
    }
    else {
        riderIsStopped = false;
    }
    if (!stopEventActive) {
        if (riderIsStopped) {
            return "";
        }
        return " hideThis";
    }
    else if (stopEventActive && riderIsStopped) {
        return " hideThis";
    }
    else {
        return "";
    }
}





function stopButtonText(riderId) {
    const filtered = stoppedRiders.filter(element => element.id === riderId);
    if (filtered.length > 0) {
        return "cncl";
    }
    else {
        return "STOP";
    }
}





function sendRiderToStart(riderId) {
    // Function places a rider in the starting box.
    riderInStartPosition = true;
    goButtonHidden = false;
    waitingId = riderId;
    console.log("Armed rider to start: " + waitingId);  // Send ID to API.
    //waitingIndex = index;   // Keep track which rider from startQue is in the startbox
    showStartQue();
    showInField();
}




function armDNF(riderId) {
    // Function to select a rider as DNF.
    // There is a delay between clicking DNF and sending the information to API.
    // When clicking DNF, first the rider is placed in armedDNF array.
    // After the delay, the rider id is send to API and rider is removed from armedDNF array.
    
    
    
    // First check is rider is already in armedDNF array. Otherwise remove it. (Cancels action.)
    if (armedDNF.find(element => element.id == riderId)) {
        armedDNF = armedDNF.filter(element => element.id != riderId);
    }
    else { // The rider is not in armedDNF array. Add it now.
        armedDNF.push({id: riderId, time: Date.now(), sent: false});
        
//        
//        if (armedDNF.length > 1) {
//            console.log("SWAP!");
//            [armedDNF[0], armedDNF[1]] = [armedDNF[1], armedDNF[0]];
//        }
//        
//        console.log(armedDNF);
        
//        console.log(1);
//        console.log(armedDNF);
        // set new entry to top WERKT NIET
//        let x = armedDNF.length;
//        let temporary = x;
//        x--;
//        while (x > 0) {
//            
//            console.log(x);
//            
//            console.log(armedDNF);
//            
//            let element = armedDNF[1];
//            
//            console.log(element);
//            
//            armedDNF[x] = armedDNF[x-1];
//            
//            console.log(armedDNF);
//            
//            armedDNF[x-1] = element;
//            
//            console.log(armedDNF);
//            
//            x--;
//        }
//        console.log(armedDNF);
    }
    showInField();
}

setInterval(dnfCheck, 1000);
setInterval(stoppedCheck, 1000);

function addSmallPenalty(riderId, row) {
    console.log("Plus Small Penalty: " + riderId);
    buttonId = 'inFieldPlusS' + row;
    feedback(buttonId, ["smallPenalty"]);
}

function minusSmallPenalty(riderId, row) {
    console.log("Remove Small Penalty: " + riderId);
    buttonId = 'inFieldMinS' + row;
    feedback(buttonId, ["smallPenalty"]);
}

function addBigPenalty(riderId, row) {
    console.log("Plus Big Penalty: " + riderId);
    buttonId = 'inFieldPlusB' + row;
    feedback(buttonId, ["bigPenalty"]);
}

function minusBigPenalty(riderId, row) {
    console.log("Remove Big Penalty: " + riderId);
    buttonId = 'inFieldMinB' + row;
    feedback(buttonId, ["bigPenalty"]);
}



function flagDSQ(riderId) {
    
    // to prevent errors during testing only:
    
    let filteredStoppedRiders = stoppedRiders.filter(element => riderId === element.id);
    if (filteredStoppedRiders.length > 0) {
        console.log("It is not possible to DSQ already stopped riders without connection to API");
        return;
    }
    
    // end
    
    
    
    let filteredInField = inField.filter(element => riderId === element.id);
    riderDetails = filteredInField[0];
    
    if (riderDetails.dsq) {
        // Cancel DSQ flag
        console.log("Candel DSQ: " + riderId);
        inField.forEach(element => {if (element.id === riderId) element.dsq = false});
        showInField();
        return;
    }
    
    // Set DSQ flag
    
    
    console.log("DSQ: " + riderId); // Send id to API.
    inField.forEach(element => {if (element.id === riderId) element.dsq = true});
    showInField();
}


function getInFieldIndex(id) {
    // Search a rider id in the inField array and return the index.
    let i = 0;
    let index = 0;
    inField.forEach(element => { if (element.id == id) {index = i;} else {i++;} });
    return index;
}




function cancelStop(riderId) {
    console.log("Cancel last stop event with rider id: " + riderId);
}




function confirmStop(riderId) {    
    // After a stop event, this function sends the correct id of the rider which passed the stop sensor to API.
    let index = getInFieldIndex(riderId);
    
    
    // First check if the rider is already selected to stop. Otherwise cancel!
    
    const filteredStoppedRiders = stoppedRiders.filter(element => element.id == riderId);
    
    if (filteredStoppedRiders.length > 0) {
        // Rider was already stopped. Cancel this action.
        cancelStop(riderId);
        stoppedRiders = stoppedRiders.filter(element => element.id != riderId);
        setTimeout(stopEvent, 1000); // add a small delay to make inField update.
        
        
        
        
/*
     * 
     * 
     * 
     *      FOR TESTING ONLY!
     *      REMOVE CODE LATER
     * 
     * 
     * 
    */



                inField.push(lastStoppedRider);



    /*
     * 
     * 
     * 
     *          END OF 'FOR TESTING ONLY'
     * 
     * 
     * 
     */
        
        
        
        
        return;
    }
    
    
    let details = inField[index];
    lastStoppedRider = details;
    details.stopTime = Date.now();
    
    stoppedRiders.push(details);
    
    /*
     * 
     * 
     * 
     *      FOR TESTING ONLY!
     *      REMOVE CODE LATER
     * 
     * 
     * 
    */



    inField = inField.filter(element => element.id != riderId);



    /*
     * 
     * 
     * 
     *          END OF 'FOR TESTING ONLY'
     * 
     * 
     * 
     */




    console.log("Last STOP event was from rider: " + riderId); // Send id to API
    stopEventActive = false;
    clearInterval(flashingInterval);
    flashingInterval = null;
    showInField();
}

function displayTime(seconds) {
    // Outputs correct time format
    var s = seconds % 60;
    var m = (seconds - s) / 60;
    if (s < 10) {
        s = "0" + s;
    }
    if (m < 10) {
        m = "0" + m;
    }
    var txt = m + ":" + s;
    return txt;
}




function feedback(id, classes) {
    // Makes buttons flash when clicked.
    // id is the button id (from HTML).
    // classes is an array which contains the classes that should be temporarily removed and placed back.
    var element = document.getElementById(id);
    element.classList.add("feedback");
    classes.forEach(cssClass => element.classList.remove(cssClass));
    setTimeout((() => {element.classList.remove("feedback");
    classes.forEach(cssClass => element.classList.add(cssClass));}), 150);
}

var flashingInterval;

function stopEvent() {
    // Calling this function will show the stop buttons in the UI table.
    
    
    // Don't execute when another stop event is active
    if (stopEventActive) {
        return;
    }
    
    ignoredStopEvent = false;
    clearTimeout(ignoreStopEventTimeout);
    ignoreStopEventTimeout = null;
    console.log("A stop event was detected.");
    flashingInterval = setInterval(flashStopButton, 500);
    stopEventActive = true;
    showInField();
}


var stopButtonFlash = false;

function flashStopButton() {
    if (stopButtonFlash) {
        stopButtonFlash = false;
    }
    else {
        stopButtonFlash = true;
    }
    
    probablyStopped = whichRiderWasMostProbablyStopped();
    showInField();
//    console.log(stopButtonFlash);
}

function whichRiderWasMostProbablyStopped() {
    let arr = inField.filter(inFieldElement =>
        {
            const filteredDNF =armedDNF.filter(armedDNFelement => armedDNFelement.id == inFieldElement.id);
            if (filteredDNF.length > 0)
                {return false;}
            else
                {return true;}
        });
    arr = sortArrayWithObjects(arr, "time", "dsc");
    let id = arr[0].id;
    return id;
}


function countdownDNS() {
    armedDNS.forEach((forEachElement, index, arr) => 
        {
            forEachElement.count--;     
            if (forEachElement.count <= 0)
            {
                flagDNS(forEachElement.id);
                armedDNS = armedDNS.filter(armedDNSelement => armedDNSelement.id != forEachElement.id);
                
                
                




                                                /*
                                    * 
                                    * 
                                    * 
                                    *      FOR TESTING ONLY!
                                    *      REMOVE CODE LATER
                                    * 
                                    * 
                                    * 
                                   */



                                   startQue = startQue.filter(startQueElement => startQueElement.id != forEachElement.id);



                                   /*
                                    * 
                                    * 
                                    * 
                                    *          END OF 'FOR TESTING ONLY'
                                    * 
                                    * 
                                    * 
                                    */
                
                
                
                
                
                
            }
        }
    );
    
    if (armedDNS.length <= 0) {        
        clearInterval(armedDNSinterval);
        armedDNSinterval = null;
    }

    showStartQue();
}

var armDNSid;
var armedDNSinterval;

function armDNS(id) {
    var filteredDNS = armedDNS.filter(element => element.id === id);
    
    if (filteredDNS.length > 0) {
        cancelDNS(id);
        return;
    }
    
    armedDNS.push({id: id, count: 10});
    armDNSid = id;
    if (armedDNSinterval == null) {
        armedDNSinterval = setInterval(countdownDNS, 1000);
    }
    showStartQue();
}


function cancelDNS(id) {
    armedDNS = armedDNS.filter(element => element.id !== id);
    if (armedDNSinterval > 0) {
        clearInterval(armedDNSinterval);
        armedDNSinterval = null;
    }
    showStartQue();
}


function flagDNS(riderId) {
    // If a rider did not appear at the starting box.
    console.log("Rider flagged as DNS: " + riderId); // Send rider id to API.
}


function dnsButtonText (id) {
    var filteredDNS = armedDNS.filter(element => element.id === id);
    if (filteredDNS.length > 0) {
        let count = filteredDNS[0].count;
        return count;
    }
    
    return "DNS";
}



function hideDNSbutton(id) { 
    var filteredDNS = armedDNS.filter(element => element.id === id);
    
    if (filteredDNS.length == 0 && armedDNSinterval > 0) {
        //return " hidden";
        return "";
    }
    else {
        return "";
    }
}



document.onkeydown = function (e) {
  // Simulate a stop event by pressing "s" button.
  
  if (e.key === "s" || e.key === "S") {
      stopEvent();
      showInField();
  }
};


function moveDownOrder(riderId) {
    console.log("Move down in starting order: " + riderId); // Send rider id to API
}

function moveUpOrder(riderId) {
    console.log("Move up in starting order: " + riderId); // Send rider id to API
}



function sortArrayWithObjects(arr, key, ascDsc = "asc") {
    let i = 0;
    let size = arr.length;
    let compare;
    
    while (i < size-1) {
        i++;
        
        if (arr[i-1][key] > arr[i][key]) {
            if (ascDsc === "asc") {
                compare = true;
            }
            else {
                compare = false;
            }
        }
        else {
            if (ascDsc === "asc") {
                compare = false;
            }
            else {
                compare = true;
            }
        }
        
        if (compare) {
            const obj = arr[i-1];
            arr = arr.filter(element => element != obj);
            i = 0;
            arr.push(obj);
        }
    }
    
    
    return arr;
}



function stopClass(riderId) {
    if (riderId != probablyStopped || !stopButtonFlash || !stopEventActive) {
        return "stop";
    }
    // changes stopButton css class to make it flash.
    else {
        return "stopFlash";
    }
}



function hideIgnoreButton() {
    if (stopEventActive || ignoredStopEvent) {
        return "";
    }
    else {
        return " hideThis";
    }
}


function ignoreButtonText() {
    if (ignoredStopEvent) {
        return "cncl";
    }
    return "IGNORE";
}


function hideDNFbutton(riderId) {
    // hides the DNF button if the rider has already stopped
    let filtered = stoppedRiders.filter(element => element.id == riderId);
    if (filtered.length > 0) {
//        return "";
        return " hideThis";
    }
    else {
        return "";
    }
}