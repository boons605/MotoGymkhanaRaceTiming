
let inField = [];
let  startQue = [];
let armedDNF = [];
let stoppedRiders = [];
let armedDNS = [];

var stopEventActive = false;
var riderInStartPosition = false;
var goButtonHidden = false;
var waitingId;
var showAll = false;
var firstUpButtonShown = false;
var ignoreDNFrule = false;
var row;
var probablyStopped;
var lastStoppedRider;
var ignoredStopEvent = false;
var delayTime = 5000;  // For testing set to low value. Live should be a larger value (around 20000).
var url = "https://localhost:53742";
var serverRequestInterval = 100;
var startTimes = [];
var apiWaitingId = "";
var unmatchedEndTimes = [];
var maxTimeValue = 10*60000 - 1; // 10 minutes minus a millisecond



// List of contesters:
var contesters = [
    "Claud van Gessel",
    "Frank Bock",
    "Arjan Geitenbeek",
    "Feike Eijlers",
    "Marie Geerlings",
    "Jan van Dieren",
    "Paul Wonka",
    "Bob Roodbaard",
    "Paul Hille",
    "Bert Schuld",
    "Martijn Stapelbroek",
    "Elias Shepherd",
    "Richard van Schouwenburg"
];

var riderNumbers = [
    1,2,3,4,5,6,7,8,9,10,11,12,13
];

function postAPI (url, data) {
    // sends POST requests to server

    let fetchData = {
        method: 'POST',
        body: JSON.stringify(data),
        headers: new Headers({
        'Content-Type': 'application/json; charset=UTF-8'
        })
    };
    
    return fetch(url, fetchData);
}





function uuidv4() {
    // create a unique id
  return ([1e7]+-1e3+-4e3+-8e3+-1e11).replace(/[018]/g, c =>
    (c ^ crypto.getRandomValues(new Uint8Array(1))[0] & 15 >> c / 4).toString(16)
  );
}




async function loadWaitingList(c) {
    
    // Is rider list empty?
    
    var response = await fetch("/racetracking/getriders");
    var data = await response.json();
    
    let txt = data;

    if (txt.length == 0) {
        // Rider list is empty! So add riders to it.

        length = c.length;
         i = 0;

         console.log(length + " contesters");

         while (i < length) {
             var data = {
                 Name: contesters[i],
                 Id: uuidv4()
             };

             console.log("Add rider: " + contesters[i]);

            await postAPI("/racetracking/rider", data);
            i++;
         }

         console.log("Riders added.");
    }
    else {
        // List already loaded, do nothing
        return;
    }
}



fetch("/racetracking/state")
    .then((response) => {
      return response.json();
    })
    .then((data) => {
        let txt = data;
        if ('Error' in data) {
            console.log('Simulation not running');
        }
        else {
            console.log('Simulation status ok!');
            loadWaitingList(contesters);
        }
    })
    .catch((error) => {
      console.log(error);
    });
    
   






var serverRiderDataTxt = "";

async function updateRiderList() {
//    console.log("Hello server. (Rider Data)");
    
    var response = await fetch("/racetracking/getRiders");
    var data = await response.json();
    
    var oldServerRiderDataTxt = serverRiderDataTxt;
    serverRiderDataTxt = JSON.stringify(data);
    
    // only continue if server data changed:
    if (oldServerRiderDataTxt === serverRiderDataTxt) {
        return;
    }
    
    
    console.log("rider update found");

    let txt = data;

    startQue = [];

    var position = 0;
    txt.forEach((contObj) => {
        let name = contObj['Name'];
        let id = contObj['Id'];

        var riderOnTrack = inField.filter((element) => element.id == id).length;

        if (riderOnTrack) {
            //return;
        }

        let index = contesters.indexOf(name);

        let nr = riderNumbers[index];

        startQue.push({nr: nr, id: id, name: name, position: position});
        position++;
    }); 

    showStartQue();
}

setInterval(updateRiderList, serverRequestInterval);





var serverStateTxt = "";
var serverPenaltiesTxt = "";

async function getDataFromServer() {
    
    var responseState = await fetch("/racetracking/state");
    var responsePenalties = await fetch("/racetracking/penalties");
    var dataState = await responseState.json();
    var dataPenalties = await responsePenalties.json();
    
    var oldServerStateTxt = serverStateTxt;
    serverStateTxt = JSON.stringify(dataState);
    
    var oldServerPenaltiesTxt = serverPenaltiesTxt;
    serverPenaltiesTxt = JSON.stringify(dataPenalties);
    
    // only continue if server data changed:
    if (oldServerStateTxt === serverStateTxt && oldServerPenaltiesTxt === serverPenaltiesTxt) {
        return;
    }
    
    
    console.log("Server update detected.");
    
    
    
    
    
    
    
    let txt = dataState;

    if (txt['waiting'] === null) {
        apiWaitingId = "";
    }
    else {
        var obj = JSON.stringify(txt);
        obj = JSON.parse(obj);
        apiWaitingId = obj['waiting']['Rider']['Id'];
    }

    let onTrack = txt['onTrack'];

    inField = [];

    onTrack.forEach((contObj) => {

        let item = contObj['Item2'];

        let name = item['Rider']['Name'];
        let id = item['Rider']['Id'];
        let startMillis = item['Microseconds'];


        let index = contesters.indexOf(name);

        let nr = riderNumbers[index];


        // Calculate displayed time:
        var t = 0;
        var it = startTimes.filter(element => element.id == id);
        if (it.length > 0) {
            var s = it[0].time;
            t = Math.floor((Date.now() - s)/1000);
        }
        
        var penalties = translatePenaltiesFromAPIdata(dataPenalties, id);

        inField.push({nr: nr, id: id, name: name, time: t, p1: penalties.p1, p3: penalties.p3, dnf: false, dsq: penalties.dsq, startMillis: startMillis});
        
        inField = sortArrayWithObjects(inField, "startMillis", "desc");


        unmatchedEndTimes = [];

        var obj = JSON.stringify(txt);
        obj = JSON.parse(obj);                
        arr = obj['unmatchedEndTimes'];

        arr.forEach(
                (element) => {
                    var ms = element.Microseconds;
                    var tid = element.EventId;
                    unmatchedEndTimes.push({time: ms, id: tid});
                }
            );

        unmatchedEndTimes = sortArrayWithObjects(unmatchedEndTimes, "time");
        
        if (unmatchedEndTimes.length > 1) {
            console.log("First stopEvent not confirmed. Second stopEvent detected. Force match with unmatchedEndTime.");
            forceMatch();
        }

        if (unmatchedEndTimes.length > 0) {
            stopEventActive = true;
            stopEvent();
        }
        else if (unmatchedEndTimes.length === 0) {
            stopEventActive = false;
        }

    }); 
    
    updateStartBox();
    showInField();
    showStartQue();
}


setInterval(getDataFromServer, serverRequestInterval);





function translatePenaltiesFromAPIdata(data, id = "") {
    
    var result = {
        "p1": 0,            // 1s penalties
        "p3": 0,            // 3s penalties
        "dsq": false    // dsq flag
    }
    
    var arr;
    
    if (id === "") {            // the data sent is an array containing only penalties from 1 id
        arr = data;
    }
    else {
        arr = data[id];         // the data sent is an object contining arrays of penalties from several id's
    }
    
    if (arr.length === 0 ) {
        // there are no penalties for this rider
        return result;
    }
    
    // count penalties
    var pp1 = arr.filter(element => element.seconds === 1).length;
    var pn1 = arr.filter(element => element.seconds === -1).length;
    var pp3 = arr.filter(element => element.seconds === 3).length;
    var pn3 = arr.filter(element => element.seconds === -3).length;

    result.p1 = pp1 - pn1; // total of 1s penalties
    result.p3 = pp3 - pn3; // total of 3s penalties
    
    return result;
}




var results = [];
var serverResultsTxt = "";
var ridersWithResults = [];

async function getResultsFromServer() {
    
    var response = await fetch("/racetracking/laps");
    var data = await response.json();
    
    var oldServerResultsTxt = serverResultsTxt;
    serverResultsTxt = JSON.stringify(data);
    
    // only continue if server data changed:
    if (oldServerResultsTxt === serverResultsTxt) {
        return;
    }
    
    ridersWithResults = [];
    startQue.forEach((rider) => {
        var id = rider.id;
        if (serverResultsTxt.search(id) >= 0) {
            ridersWithResults.push(id);
        }
    });
    
    // create an object with all the date from finished riders
    results = [];
    data.forEach(element => results.push(constructRiderResult(element)));
    
    results = sortArrayWithObjects(results, "result", "asc");
    
    for (i=0; i < results.length; i++) {
        results[i].position = i+1;
    }
    
    
    // correct for shared position (equal results)
    var prevResult = 0;
    var prevPosition = 0;
    var i = 0;
    results.forEach((result) => {
        if (result.result === prevResult) {
            results[i].position = prevPosition;
        }
        prevResult = result.result;
        prevPosition = result.position;
        i++;
    });
    
    
    showResults();
    showStartQue();
    
}

setInterval(getResultsFromServer, serverRequestInterval);




function updateStartBox() {
    if (apiWaitingId != "") {
        riderInStartPosition = true;
        goButtonHidden = true;
        waitingId = apiWaitingId;
        
        ////231101 showStartQue();
    }
    else if (goButtonHidden == true) {
        goButtonHidden = false;
        riderInStartPosition = false;
        waitingId = "";
    }
    

    //231101 showInField();
    //231101 showStartQue();
    
}

//setInterval(updateStartBox, serverRequestInterval);




//231101 showInField();
//231101 showStartQue();



function riderDNF (filteredDNFelement) { console.log("Kees");
    // Send id to API
    console.log("Set rider as DNF: ", filteredDNFelement.id);
    //change 'sent' variable to 'true' to avoid sending same information to API more then once.
    armedDNF.forEach((armedDNFelement, index) => {if (armedDNFelement.id === filteredDNFelement.id) {armedDNF[index].sent = true} });
    
    

    var riderId = filteredDNFelement.id;
    console.log("DNFid to API: " + riderId)
    
    var data = {
        "RiderId": riderId,
        "StaffName": "UI"
    }
    
    postAPI("racetracking/dnf", data);
    
    
    
    
    
    
    
    
    
    
    
    
    // delete the element from armedDNF.
    armedDNF = armedDNF.filter(armedDNFelement => filteredDNFelement.id != armedDNFelement.id);
    //231101 showInField();
}


function dnfCheck() { 
    // When selected, a rider gets a DNF status within UI. These riders are stored in armedDNF array.
    // After a delay of xxx milliseconds, the DNF will be communicated to API. And deleted from armedDNF array.
    
    // Search for DNF riders who are there for more then xxx milliseconds:
    const filteredDNF = armedDNF.filter(element => element.time < Date.now()-delayTime && element.sent === false);

    // Communicate to API:
    filteredDNF.forEach( (rider) => {
        var id = rider.id;
        var data = {
            RiderId: id,
            StaffName: "UI"
        };
        postAPI("/racetracking/dnf", data);
        console.log("Send to server DNF flag for id: " + id);
        armedDNF = armedDNF.filter(element => element.id != id);
    }
            );
}

setInterval(dnfCheck, 500);


function stoppedCheck() {
    stoppedRiders = stoppedRiders.filter(element => element.stopTime > Date.now() - (delayTime + 10000));   // Set to 5000 for TESTING purposes. Should be longer.
    //231101 showInField();
}




function showInField() {
    row = 0; // Resets rownumber
    document.getElementById('inField').innerHTML = "";  // Empty the table first
    showStartPosition();    // Shows the rider who is in the start box
    
    inField = sortArrayWithObjects(inField, "time");
    timerLocation = [];
    inField.forEach(addRowInField);     // Add all the riders row by row to the HTML table  who are now riding (in the field)
    
    
//        
//    return;    
//        
//        
//    // At the bottom of inField table, show the stopped riders and riders with a DNF flag:
//    
//    // Combine info from dnfRiders and stoppedRiders:
//    let vanishingRiders = [];
//    armedDNF.forEach(       element => vanishingRiders.push({id: element.id, time: element.time,                type: "dnf"}) );
//    //stoppedRiders.forEach( element => vanishingRiders.push({id: element.id, time: element.stopTime,    type: "stopped"}) );
//    
//    // Sort the array to display all the riders in order of time:
//    vanishingRiders = sortArrayWithObjects(vanishingRiders, "time", "dsc");
//    
//    // Now display:
//    vanishingRiders.forEach(vanishingElement => 
//        {
//            if (vanishingElement.type === "stopped") {
//                let filtered = stoppedRiders.filter(stoppedRidersElement => stoppedRidersElement.id === vanishingElement.id);
//                addRowInField(filtered[0]);
//            }
//            else if (vanishingElement.type === "dnf") {
//                // DNF riders are still 'in the field'. Therefore, their info must be retreived from 'inField'.
//                let filtered = inField.filter(inFieldElement => inFieldElement.id === vanishingElement.id);
//                ignoreDNFrule = true;   // Otherwise the DNF rider cannot be displayed (see addRowInField()).
//                addRowInField(filtered[0]);
//                ignoreDNFrule = false;  // Reset variable.
//            }
//        }
//    );
    
    
   
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
        //231101 showInField();
        return;
    }
    
    
    //20231120 stopEventActive = false;
    clearInterval(flashingInterval);
    flashingInterval = null;
    
    ignoredStopEvent = true;
    ignoreStopEventTimeout = setTimeout(() => ignoredStopEvent = false, delayTime);
    
    console.log("Ignore Stop Event.");
    //231101 showInField();
}

function showStartQue() {
    row = 0; // Resets row number
    
    // Display all the rider who still have to start.
    
    var el = document.getElementById('startQue');
    
    firstUpButtonShown = false; // Hide the first 'up' button
    
    el.innerHTML = "";  // Empty the HTML table
    ridersDisplayedInStartQue = 0;
    startQue.forEach(addRowStartQue);   // Add all the riders row by row to the table.
    
    console.log(startQue.length - results.length - inField.length);
    if (startQue.length - results.length - inField.length < 5) {
        return;
    }
    
    // Create the "SHOW ALL" function
    if (!showAll) {
        el.innerHTML +=
        '<tr><td colspan="2"></td><td colspan="7" class="btn1 showAll" onpointerdown="showCompleteStartQue()">&#8595; SHOW ALL</td></tr>';
    }
    else {
        el.innerHTML +=
        '<tr><td colspan="2"></td><td colspan="7" class="btn1 showAll" onpointerdown="makeStartQueSmaller()">&#8593; SHOW LESS</td></tr>';  
    }
}



function stopwatch () {
    inField.forEach((rider) => {
        arr = startTimes.filter(element => (element.id === rider.id));
        if (arr.length === 0) {
            startTimes.push({id: rider.id, time: Date.now()});
        }
        var t = startTimes.filter(element => (element.id === rider.id));
        var ms = Date.now() - t[0].time;;
        var display = displayTime(ms);
        
        // If rider already stopped, get the actual result from timer
        if (stoppedRiders.filter(element => element.id === rider.id).length > 0) {
            ms = results[rider.id];
            display = displayTime(ms, "ms");
        }
        
        document.getElementById('inFieldTime' + timerLocation[rider.id]).innerHTML = display;
    });
}

setInterval(stopwatch, 100);




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
                '<td id="greenLight" class="tbCell turnLightGreen btn'+ hideGoButton() +'" colspan=3 onpointerdown="turnOnGreenLight(\'' + riderInfo.id + '\')">GO!</td>' +
                '<td id="cancelGreenLight" class="tbCell btn cancelGreenLight" colspan=3  onpointerdown="cancelStart()">CANCEL</td>' +
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
    console.log("Turn on green light for: " + id);
    
    
    // send to API
    let url = "/RaceTracking/RiderReady?riderId=" + waitingId;
    fetch(url)
            .then((response) => {
                console.log("Told server rider ready with id: " + waitingId);
            })
            .catch((error) => {
              console.log(error);
            });
    
    
    
//    goButtonHidden = true;
//
//    
//    //231101 showInField();
//    //231101 showStartQue();
}

function cancelStart() {    // Removes rider from start Box
    riderInStartPosition = false;
    goButtonHidden = false;
    console.log("Clear Start Box");
    
    //231101 showInField();
    //231101 showStartQue();
    
    fetch("/racetracking/clearStartBox")
        .then( (response) => {
            console.log("Told server that startbox is clear.")}
        )
        .catch( (error) => {
            console.log(error)}
        );

     showInField();
     showStartQue();
}


ridersDisplayedInStartQue = 0;

function addRowStartQue(currentValue, index, arr) {
    // If the rider is in start position, don't display it here.
    if (waitingId === currentValue['id'] && riderInStartPosition === true) {
        return;
    }
    
    // If the rider in the field, don't display it in the startlist.
    if (inField.filter(element => (element.id ===currentValue['id'])).length > 0) {
        return;
    }
    
    
    if (ridersWithResults.indexOf(currentValue.id) >= 0) {
        // Rider already in with result
        return;
    }
    
    
    
    if (ridersDisplayedInStartQue > 3  && showAll === false) {
        return; // Don't show more then 4 items for the compact view
    }
    
    ridersDisplayedInStartQue++;
    
    document.getElementById('startQue').innerHTML += 
          '<tr class="row">' +
                '<td id="startQueNr' + row +'" class="tbCell riderNr">' +
                    currentValue.nr +
                '</td>' +
                '<td id="startQueName' + row +'" class="tbCell riderName">' +
                    currentValue.name +
                '</td>' +

                '<td id="up' + row +'" class="tbCell btn1 positionChange' + hideFirstUpButton(index) + '" onpointerdown="moveUpOrder(\'' + currentValue.id + '\', ' + row + ')")">' +
                    '&#8743;' +
                '</td>' +
                
                '<td class="empty">' +
                    
                '</td>' +

                '<td id="down' + row +'" class="tbCell btn1 positionChange' + hideLastDownButton(index) + '" onpointerdown="moveDownOrder(\'' + currentValue.id + '\', ' + row + ')")">' +
                    '&#8744;' +
                '</td>' +
                
                '<td class="empty">' +
                    
                '</td>' +
               

                '<td id="startQueStart' + row +'" class="tbCell btn1 startButton' + hideGoButton() + '" onpointerdown="sendRiderToStart(\'' + currentValue.id + '\', ' + row + ')")">' +
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



var timerLocation = [];
var probablyStoppedRow;

function addRowInField(currentValue, index, arr) {
    
    // Dont't show a rider if he has a DNF status. These riders will be shown last. To make this possible, 'ignoreDNFrule' can be switched to 'true'.
    
//    if (armedDNF.find(element => element.id == currentValue.id) && !ignoreDNFrule) {
//        return;
//    }
    
    
    // stores the location of the rider stopwatch times so it can be displayed correctly
    timerLocation[currentValue.id] = row;
    
    probablyStoppedId = whichRiderWasMostProbablyStopped();
    
    if (currentValue.id === probablyStoppedId) {
        probablyStoppedRow = row;
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

                '<td id="inFieldStop' + row +'" class="tbCell stop btn' + checkStopEvent(currentValue.id) + '" onpointerdown="confirmStop(\'' + currentValue.id + '\', ' + row + ')">' +
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
        
        // Record the time that rider started (i.e. first time appeared in the field)
//        var t = Date.now();
//        var it = startTimes.filter(element => element.id == waitingId);
//        if (it.length > 0) {
//            startTimes[index].time = t; // update existing id
//        }
//        else {
//            startTimes.push({id: waitingId, time: t}); // create new entry
//        }
                
        
        // Could be removed but smoothes out user experience:
         startQue = startQue.filter(element => element.id !== waitingId);  
        
        
        
        
        
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
        
        
        
        
                //231101 showStartQue();
        
        
        
        
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
    const filteredRider = stoppedRiders.filter(element => element.id === riderId);
    if (filteredRider.length > 0) {
        riderIsStopped = true;
    }
    else {
        riderIsStopped = false;
    }
    
    
    let riderIsDNF;
    const filteredDNF = armedDNF.filter(element => element.id === riderId);
    if (filteredDNF.length > 0) {
        riderIsDNF = true;
    }
    else {
        riderIsDNF = false;
    }
    
    
    
    if (!stopEventActive) {
        return " hideThis";
    }
    else if (riderIsDNF) {
        return " hideThis";
    }
    else if (armStop && !riderIsStopped) {
        return " hideThis";
    }
    else {
        return "";
    }
    
    return;
    
//    if (!stopEventActive) {
//        if (riderIsStopped) {
//            return "";
//        }
//        return " hideThis";
//    }
//    else if (stopEventActive && riderIsStopped) {
//        return "";
//    }
//    else {
//        return " hideThis";
//    }
}



//setInterval(( () => {console.log(stoppedRiders.length)} ), 1000);

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
    console.log("Armed rider to start: " + waitingId);
    

    showStartQue();
    showInField();
}




function armDNF(riderId) {
    console.log("Set to DNF: " + riderId);
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
    }
    showInField();
}

//setInterval(dnfCheck, 1000);
//setInterval(stoppedCheck, 1000);

function addSmallPenalty(riderId, row) {
    console.log("Plus Small Penalty: " + riderId);
    buttonId = 'inFieldPlusS' + row;
    feedback(buttonId, ["smallPenalty"]);
    
    
    //Send to server:
    var data = {
        RiderId: riderId,
        StaffName: "UI",
        Reason: "",
        Seconds: 1
    };
    
    postAPI("/racetracking/penalty", data);
    
    
}

function minusSmallPenalty(riderId, row) {
    console.log("Remove Small Penalty: " + riderId);
    buttonId = 'inFieldMinS' + row;
    feedback(buttonId, ["smallPenalty"]);
    
    
    //Send to server:
    var data = {
        RiderId: riderId,
        StaffName: "UI",
        Reason: "",
        Seconds: -1
    };
    
    postAPI("/racetracking/penalty", data);
    
    
}

function addBigPenalty(riderId, row) {
    console.log("Plus Big Penalty: " + riderId);
    buttonId = 'inFieldPlusB' + row;
    feedback(buttonId, ["bigPenalty"]);
    
    
    
    
    //Send to server:
    var data = {
        RiderId: riderId,
        StaffName: "UI",
        Reason: "",
        Seconds: 3
    };
    
    postAPI("/racetracking/penalty", data);
    
    
    
}

function minusBigPenalty(riderId, row) {
    console.log("Remove Big Penalty: " + riderId);
    buttonId = 'inFieldMinB' + row;
    feedback(buttonId, ["bigPenalty"]);
    
    
    
    //Send to server:
    var data = {
        RiderId: riderId,
        StaffName: "UI",
        Reason: "",
        Seconds: -3
    };
    
    postAPI("/racetracking/penalty", data);
    
    
    
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
        //231101 showInField();
        return;
    }
    
    // Set DSQ flag
    
    
    console.log("DSQ: " + riderId); // Send id to API.
    
    var data = {
    "RiderId": riderId,
    "StaffName": "UI",
    "Reason": ""
    }
    
    postAPI("/racetracking/dsq", data);
    
    inField.forEach(element => {if (element.id === riderId) element.dsq = true});
    //231101 showInField();
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
    clearTimeout(deleteMatch[riderId]);
    deleteMatch[riderId] = null;
    armStop = false;
    stoppedRiders.pop();
    stopEvent();
    showInField();
}



async function matchEndTimeToServer(riderId, stopTimeId) {
    
    url = "/racetracking/matchendtime?riderId=" + riderId + "&timeId=" + stopTimeId;

       fetch(url)
               .then((response) => {
                   console.log("Sent stopped rider id to server.");
                    armStop = false;
                    showInField();
                })
               .catch((error) => {
                   console.log(error);
               });
}


var deleteMatch = [];
var armStop = false;
var results = [];

function confirmStop(riderId) {   
    console.log("confirm stop");
    // After a stop event, this function sends the correct id of the rider which passed the stop sensor to API.
    let index = getInFieldIndex(riderId);
    
    
    // First check if the rider is already selected to stop. Otherwise cancel!
    
    const filteredStoppedRiders = stoppedRiders.filter(element => element.id == riderId);
    
    if (filteredStoppedRiders.length > 0) {
        // Rider was already stopped. Cancel this action.
        cancelStop(riderId);
        stoppedRiders = stoppedRiders.filter(element => element.id != riderId);
        stopEvent();
        
        
        return;
    }
    
    
    let details = inField[index];
    lastStoppedRider = details;
    details.stopTime = Date.now();
    
    stoppedRiders.push(details);

    var usStart = details.startMillis;
    var usEnd = unmatchedEndTimes[0]['time'];
    var us = usEnd - usStart;
    var ms = Math.floor(us/1000);
    
    results[riderId] = ms;

    console.log("Last STOP event was from rider: " + riderId + ". Ride time: " + ms + "ms."); // Send id to API
    
    stopTimeId = unmatchedEndTimes[0]['id'];
        
    deleteMatch[riderId] = setTimeout(matchEndTimeToServer, delayTime, riderId, stopTimeId);
    armStop = true;
    
    showInField();
}



function forceMatch() {
    // Execute when there are more then 1 unmatchedEndTimes
    
    if (armStop) {
        // execute the planned stop immediately
        clearTimeout(deleteMatch[lastStoppedRider.id]);
        deleteMatch[lastStoppedRider.id] = null;
        matchEndTimeToServer(lastStoppedRider.id, unmatchedEndTimes[0]['id']);
    }
    else {
        // match unmatched time with most probable rider
        var id = whichRiderWasMostProbablyStopped();
        matchEndTimeToServer(id, unmatchedEndTimes[0]['id']);
    }
}






function displayTime(ms, format = "s") {
    // Outputs correct time format
    
    if (ms > maxTimeValue) {    // limits the value of time
        ms = maxTimeValue;
    }
    
    var seconds = Math.floor(ms/1000);
    ms -= seconds * 1000;
    
    strMs = "" + ms;
    
    if (ms < 100) {
        strMs = "0" + strMs;
    }
    if (ms < 10) {
        strMs = "0" + strMs;
    }
    
    var s = seconds % 60;
    var m = (seconds - s) / 60;
    if (s < 10) {
        s = "0" + s;
    }
    var txt = m + ":" + s;
    
    if (format === "s") {
        return txt;
    }
    else if (format === "ms") {
        return txt + "." + strMs;
    }
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
    //20231120  stopEventActive = true;
    //231101 showInField();
}



function flashStopButton() {
    if (!stopEventActive) {
        return;
    }
  
    
    var row = probablyStoppedRow;
    
    if (armStop) {
        // this means the stopEvent was confirmed by user, so no need to flash
        return;
    }
    
    var html = document.getElementById('inFieldStop' + row);
    html.classList.toggle("stop");
    html.classList.toggle("stopFlash");
}

setInterval(flashStopButton,500);




function whichRiderWasMostProbablyStopped() {
    let arr = inField.filter(inFieldElement =>
        {
            const filteredDNF =armedDNF.filter(armedDNFelement => armedDNFelement.id == inFieldElement.id);
            if (filteredDNF.length > 0)
                {return false;}
            else
                {return true;}
        });
        
    let id;
    if (typeof arr[0] !== "undefined") {
        arr = sortArrayWithObjects(arr, "startMillis");
        id = arr[0].id;
    }
    else {
        id = "";
    }
//    console.log("MOST PROBABLY STOPPED ID: " + id);
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

    //231101 showStartQue();
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
    //231101 showStartQue();
}


function cancelDNS(id) {
    armedDNS = armedDNS.filter(element => element.id !== id);
    if (armedDNSinterval > 0) {
        clearInterval(armedDNSinterval);
        armedDNSinterval = null;
    }
    //231101 showStartQue();
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


// For testing only:

document.onkeydown = async function (e) {
 //  Send a manual event trigger to server by pressing a key.
 
 
    let formData = new FormData();
  
    if (e.key === "o" || e.key === "O") {
        console.log('Manual Stop Event');

        formData.set('gateId', 1);
    }
    else if (e.key === "a" || e.key === "A") {
          console.log('Manual Start Event');

           formData.set('gateId', 0);
      }
      else {
          return;
      }

    let response = await fetch('/debug/triggertiminggate', {
        method: 'POST',
        body: formData
    });
     
};


function moveDownOrder(riderId) {
    console.log("Move down in starting order: " + riderId); // Send rider id to API
    
    orderToServer(riderId, 1);
}

function moveUpOrder(riderId) {
    console.log("Move up in starting order: " + riderId); // Send rider id to API
    
    orderToServer(riderId, -1);
}


function orderToServer (riderId, direction) {
    // to move DOWN order, make direction +1, to move UP order make direction -1.
    var filteredStartQue =  startQue.filter((element) => element.id === riderId);
//    console.log(filteredStartQue);
    var currentPosition = filteredStartQue[0].position;
    
    var newPosition = currentPosition + direction;
    console.log("NEW POSITION: " + newPosition);
    
    var url = "/racetracking/ChangeStartingOrder?riderId=" + riderId + "&newPosition=" + newPosition;
    
    fetch(url)
            .catch( (error) => {
                console.log(error);
    });
}



function sortArrayWithObjects(arr, key, ascDsc = "asc") {
    let i = 0;      // start at the beginning of the array
    let size = arr.length;
    let compare;
    
    while (i < size-1) {
        i++;    // keep going towards the end of the array until every item is in order.
        
        var obj0 = arr[i-1][key];
        var obj1 = arr[i][key];
        
        
        // compare = true means the two objects are in the wrong order.
        // compare = false means the objects are placed correctly regarding each other and they don't have to be moved.
        
        if (obj0 === obj1) {
            compare = false;
        }
        else if (obj0 > obj1) {
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
            arr = arr.filter(element => element !== obj); // remove the object from the array
            arr.push(obj); // place the object thats in the wrong position at the end of the array
            i = 0;  // Back to the start of the array
        }
    }
    
    
    return arr;
}






function hideIgnoreButton() {
    if (stopEventActive && !armStop) {
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




function showResults() {
    
    if (results.length === 0) {
        return;
    }
    
    var txt = "";
    
    txt += "<tr><th colspan=11>Order by: <div class=\"btn order\" onpointerdown=\"orderByFinishedTime()\">Recent Finish</div> <div class=\"btn order\" onpointerdown=\"orderByResult()\">Result</div></th></tr><tr>"
         + "<th>#</th>"
        + "<th>rider</th>"
        + "<th>lap time</th>"
        + "<th>1s</th>"
        + "<th>3s</th>"
        + "<th>pen</th>"
        + "<th>XXX</th>"
        + "<th>result</th>"
        + "<th>pos</th>"
         + "</tr>";
    
    var order = "asc";
    if (orderResultsBy === "timestamp") {
        order = "dsc";
    }
    results = sortArrayWithObjects(results, orderResultsBy, order);
    
    results.forEach((result) => {
        var flag;
        var p1 = result.p1;
        var p3 = result.p3;
        var penaltyTime = result.penaltyTime;
        
        flag = "-";
        
        if (result.dns === true) {
            flag = "DNS";
        }
        else if (result.dnf === true) {
            flag = "DNF";
        }
        else if (result.dsq === true) {
            flag = "DSQ";
        }
        
        if (p1 === 0) {
            p1 = "-";
        }
        
        if (p3 === 0) {
            p3 = "-";
        }
        
        if (penaltyTime === 0) {
            penaltyTime = "";
        }
        else {
            penaltyTime = "+" + penaltyTime + "s";
        }
        
        txt += "<tr><td>";
        txt += result.nr + "</td><td>";
        txt += result.name + "</td><td>";
        txt += displayTime(result.lapTime, "ms") + "</td><td>";
        txt += p1 + "</td><td>";
        txt += p3 + "</td><td>";
        txt += penaltyTime + "</td><td>";
        txt += flag + "</td><td>";
        txt += displayTime(result.result, "ms") + "</td><td>";
        txt += result.position + "</td>";
        txt += "</tr>";
    });
    
    document.getElementById('results').innerHTML = txt;
}


function constructRiderResult(data) {
    var riderResult = {
        "id": "",
        "name": "",
        "nr": 0,
        "lapTime": 0,
        "p1": 0,
        "p3": 0,
        "penaltyTime": 0,
        "dnf": false,
        "dsq": false,
        "result": 0,
        "timestamp": "",
        "position": 0
    };
    
//    console.log(data);
    
    riderResult.dsq = data.disqualified;
    var penalties = translatePenaltiesFromAPIdata(data.penalties);
    riderResult.p1 = penalties.p1;
    riderResult.p3 = penalties.p3;
    riderResult.penaltyTime = riderResult.p1 + 3 * riderResult.p3;  // value in seconds
    riderResult.name = data.rider.id;
    riderResult.name = data.rider.name;
    let index = contesters.indexOf(riderResult.name);
    let nr = riderNumbers[index];
    riderResult.nr = nr;
    riderResult.timestamp = data.end.time;
    if (data.end.type === "ManualDNFEvent") {
        riderResult.dnf = true;
        riderResult.lapTime = maxTimeValue;
    }
    else {
        riderResult.lapTime = Math.floor(data.end.lapTime/1000);
    }
    if (riderResult.lapTime > maxTimeValue) {
        riderResult.lapTime = maxTimeValue;
    }
    riderResult.result = riderResult.lapTime + riderResult.penaltyTime * 1000;
    if (riderResult.dsq === true || riderResult.dnf === true) {
        riderResult.result = maxTimeValue;
    }
    if (riderResult.result > maxTimeValue) {
        riderResult.result = maxTimeValue;
    }


    return riderResult;
}


var orderResultsBy = "timestamp";

function orderByFinishedTime() {
    console.log("Order by finished time");
    orderResultsBy = "timestamp";
    showResults();
}

function orderByResult() {
    console.log("Order by result");
    orderResultsBy = "position";
    showResults();
}