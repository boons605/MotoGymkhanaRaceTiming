
const inField = [];
const startQue = [];
let armedDNF = [];

var stopEventActive = false;
var riderInStartPosition = false;
var goButtonHidden = false;
var waitingId;
var waitingIndex;
var showAll = false;
var firstUpButtonShown = false;
var ignoreDNFrule = false;
var row;

function uuidv4() {
    // create a unique id
  return ([1e7]+-1e3+-4e3+-8e3+-1e11).replace(/[018]/g, c =>
    (c ^ crypto.getRandomValues(new Uint8Array(1))[0] & 15 >> c / 4).toString(16)
  );
}



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
    // delete the element from armedDNF. But add delay to refresh information in inField
    setTimeout(() => {armedDNF = armedDNF.filter(armedDNFelement => filteredDNFelement.id != armedDNFelement.id); console.log("DELETED"); showInField();}, 1000);
    showInField();
}


function dnfCheck() {
    // When selected, a rider gets a DNF status within UI. These riders are stored in armedDNF array.
    // After a delay of xxx milliseconds, the DNF will be communicated to API. And deleted from armedDNF array.
    
    // Search for DNF riders who are there for more then xxx milliseconds:
    const filteredDNF = armedDNF.filter(element => element.time < Date.now()-10000 && element.sent === false);

    // Communicate to API:
    filteredDNF.forEach(riderDNF);
}




function showInField() {
    row = 0; // Resets rownumber
    document.getElementById('inField').innerHTML = "";  // Empty the table first
    showStartPosition();    // Shows the rider who is in the start box
    inField.forEach(addRowInField);     // Add all the riders row by row to the HTML table  who are now riding (in the field)
    
    
    
    // Riders with a DNF status will be show at the bottom.
    const dnfRiders = [];
    // Filter the rider in the field
    inField.forEach((element) => {if (armedDNF.find(armedDNFelement => armedDNFelement.id == element.id)) { dnfRiders.push(element) } } );
    
    ignoreDNFrule = true;   // Otherwise the DNF rider cannot be displayed (see addRowInField()).
    dnfRiders.forEach(addRowInField);   // Add the DNF riders row by row to the HTML table.
    ignoreDNFrule = false;  // Reset variable.
    
    // Create the IGNORE button:
    document.getElementById("inField").innerHTML +=
    '<tr><td colspan="15"></td><td class="btn1 ignore' + checkStopEvent() + '" onclick="ignoreStopEvent()">IGNORE</td></tr>';
}

function ignoreStopEvent() {
    // The last stop event was not a valid stop event and should be ignored.
    stopEventActive = false;
    console.log("Ignore Stop Event.");
    showInField();
}

function showStartQue() {
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
    
    if (startQue[waitingIndex].id != waitingId) { // Just an extra check to be sure nothing went wrong. 
        return;
    }
    else {
        index = waitingIndex;   // 
    }
    
    // There must be a rider in start position, so below code will be executed:
    
    document.getElementById('inField').innerHTML += 
            '<tr class="row">' +
                '<td id="startBoxNr" class="tbCell riderNr">' +
                    startQue[index].nr +
                '</td>' +
                '<td id="startBoxName' + index +'" class="tbCell riderName startBoxName">' +
                    startQue[index].name +
                '</td>' +
                '<td id="greenLight" class="tbCell turnLightGreen btn'+ hideGoButton() +'" colspan=3 onClick="turnOnGreenLight(\'' + startQue[index].id + '\')">GO!</td>' +
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
    showInField();
}

function cancelStart() {    // Removes rider from start Box
    riderInStartPosition = false;
    console.log("Clear Start Box");
    showInField();
    showStartQue();
}


function addRowStartQue(currentValue, index, arr) {
    // If the rider is in start position, don't display it here.
    if (waitingId === currentValue['id'] && riderInStartPosition === true) {
        return;
    }
    
    if (index > 3 && showAll === false) {
        return; // Don't show more then 4 items for the compact view
    }
    
    document.getElementById('startQue').innerHTML += 
          '<tr class="row">' +
                '<td id="startQueNr' + index +'" class="tbCell riderNr">' +
                    startQue[index].nr +
                '</td>' +
                '<td id="startQueName' + index +'" class="tbCell riderName">' +
                    startQue[index].name +
                '</td>' +

                '<td id="up' + index +'" class="tbCell btn1 positionChange' + hideFirstUpButton(index) + '" onClick="moveUpOrder(' + index + ')">' +
                    '&#8743;' +
                '</td>' +
                
                '<td class="empty">' +
                    
                '</td>' +

                '<td id="down' + index +'" class="tbCell btn1 positionChange' + hideLastDownButton(index) + '" onClick="moveDownOrder(' + index + ')">' +
                    '&#8744;' +
                '</td>' +
                
                '<td class="empty">' +
                    
                '</td>' +
               

                '<td id="startQueStart' + index +'" class="tbCell btn1 startButton" onClick="sendRiderToStart(' + index + ')">' +
                    'START' +
                '</td>' +
                
                '<td class="empty">' +
                    
                '</td>' +

                '<td id="startQueDNS' + index +'" class="tbCell btn1 buttonDNS" onclick="flagDNS(' + index + ')">' +
                    'DNS' +
                '</td>' +
            '</tr>';
}


var isDNF = false;



function addRowInField(currentValue, index, arr) {
    
    // Dont't show a rider if he has a DNF status. These riders will be shown last. To make this possible, 'ignoreDNFrule' can be switched to 'true'.
    
    if (armedDNF.find(element => element.id == inField[index].id) && !ignoreDNFrule) {
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

                '<td id="inFieldPlusS' + row +'" class="tbCell btn smallPenalty" onclick="addSmallPenalty(\'' + currentValue.id + '\', ' + row + ')">' +
                    '+1s' +
                '</td>' + 
                
                '<td id="inFieldS" class="tbCell penaltyCounter">' +
                    currentValue.p1 +
                '</td>' +

                '<td id="inFieldMinS' + row +'" class="tbCell btn smallPenalty" onclick="minusSmallPenalty(\'' + currentValue.id + '\', ' + row + ')">' +
                    "-1s" +
                '</td>' +

                '<td class="empty tbCell">' +

                '</td>' +

                '<td id="inFieldPlusB' + row +'" class="tbCell btn bigPenalty" onclick="addBigPenalty(\'' + currentValue.id + '\', ' + row + ')">' +
                    "+3s" +
                '</td>' +
                
                '<td id="inFieldB" class="tbCell penaltyCounter">' +
                    currentValue.p3 +
                '</td>' +

                '<td id="inFieldMinB' + row +'" class="tbCell btn bigPenalty" onclick="minusBigPenalty(\'' + currentValue.id + '\', ' + row + ')">' +
                    "-3s" +
                '</td>' +

                '<td class="empty tbCell">' +

                '</td>' +

                '<td id="inFieldDNF' + row +'" class="tbCell btn dnf' + getDNFstatus(currentValue.id) + '" onclick="armDNF(\'' + currentValue.id + '\', ' + row + ')">' +
                    getDNFbuttonText(currentValue.id) +
                '</td>' +

                '<td class="empty tbCell">' +

                '</td>' +

                '<td id="inFieldDSQ' + row +'" class="tbCell btn dsq' + isDSQ(currentValue.dsq) + '" onclick="flagDSQ(\'' + currentValue.id + '\', ' + row + ')">' +
                    getDSQbuttonText(currentValue.dsq) +
                '</td>' +

                '<td class="empty tbCell">' +

                '</td>' +

                '<td id="inFieldStop' + row +'" class="tbCell btn stop' + checkStopEvent() + '" onclick="confirmStop(\'' + currentValue.id + '\', ' + row + ')">' +
                    "Stop" +
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
        waitingId = "";
        return true;
    }
    else {
        return false;
    }
}


function checkStopEvent() {
    if (!stopEventActive) {
        return " hideThis";
    }
    else {
        return "";
    }
}

function sendRiderToStart(index) {
    // Function places a rider in the starting box.
    riderInStartPosition = true;
    goButtonHidden = false;
    waitingId = startQue[index].id;
    console.log("Armed rider to start: " + waitingId);  // Send ID to API.
    waitingIndex = index;   // Keep track which rider from startQue is in the startbox
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

setInterval(dnfCheck, 100);

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
    var i = 0;
    index = getInFieldIndex(riderId);
    console.log(index);
    
    if (inField[index].dsq) {
        // Cancel DSQ flag
        console.log("Candel DSQ: " + riderId);
        inField[index].dsq = false;
        showInField();
        return;
    }
    
    // Set DSQ flag
    console.log("DSQ: " + riderId); // Send id to API.
    inField[index].dsq = true;
    showInField();
}


function getInFieldIndex(id) {
    // Search a rider id in the inField array and return the index.
    let i = 0;
    let index = 0;
    inField.forEach(element => { if (element.id == id) {index = i;} else {i++;} });
    return index;
}




function confirmStop(riderId) {
    // After a stop event, this function sends the correct id of the rider which passed the stop sensor to API.
    stopEventActive = false;
    console.log("Last STOP event was from rider: " + riderId); // Send id to API
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


function stopEvent() {
    // Calling this function will show the stop buttons in the UI table.
    console.log("A stop event was detected.");
    stopEventActive = true;
    showInField();
}


function flagDNS(index) {
    // If a rider did not appear at the starting box.
    riderId = startQue[index].id;
    console.log("Rider flagged as DNS: " + riderId); // Send rider id to API.
}



document.onkeydown = function (e) {
  // Simulate a stop event by pressing "s" button.
  if (e.key === "s") {
      stopEvent();
      showInField();
  }
};


function moveDownOrder(index) {
    riderId = startQue[index].id;
    console.log("Move down in starting order: " + riderId); // Send rider id to API
}

function moveUpOrder(index) {
    riderId = startQue[index].id;
    console.log("Move up in starting order: " + riderId); // Send rider id to API
}