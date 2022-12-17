
const inField = [];
const startQue = [];

inField.push({nr: 9, name: "Paul Hille", time: 30, p1: 0, p3: 0, dnf: false, dsq: false});

inField.push({nr: 18, name: "Richard van Schouwenburg", time: 145, p1: 0, p3: 0, dnf: false, dsq: false});

startQue.push({nr: 7, name: "Martijn Stapelbroek"});
startQue.push({nr: 11, name: "Romke Roodbaard"});
startQue.push({nr: 5, name: "Bert Schuld"});
startQue.push({nr: 45, name: "Marie Geerlings"});

showInField();
showStartQue();


function showInField() {
    document.getElementById('inField').innerHTML = "";
    inField.forEach(addRowInField);
    document.getElementById("inField").innerHTML +=
    '<tr><td colspan="15"></td><td class="btn1 ignore">IGNORE</td></tr>';
}

function showStartQue() {
    document.getElementById('startQue').innerHTML = "";
    startQue.forEach(addRowStartQue);
    document.getElementById("startQue").innerHTML +=
    '<tr><td colspan="2"></td><td colspan="7" class="btn1 showAll">&#8595; SHOW ALL</td></tr>';
}

function addRowStartQue(currentValue, index, arr) {
    document.getElementById('startQue').innerHTML += 
          '<tr class="row">' +
                '<td id="startQueNr' + index +'" class="tbCell riderNr">' +
                    startQue[index].nr +
                '</td>' +
                '<td id="startQueName' + index +'" class="tbCell riderName">' +
                    startQue[index].name +
                '</td>' +

                '<td id="up' + index +'" class="tbCell btn1 positionChange">' +
                    '&#8743;' +
                '</td>' +
                
                '<td class="empty">' +
                    
                '</td>' +

                '<td id="down' + index +'" class="tbCell btn1 positionChange">' +
                    '&#8744;' +
                '</td>' +
                
                '<td class="empty">' +
                    
                '</td>' +
               

                '<td id="startQueStart' + index +'" class="tbCell btn1 startButton">' +
                    'START' +
                '</td>' +
                
                '<td class="empty">' +
                    
                '</td>' +

                '<td id="startQueDNS' + index +'" class="tbCell btn1 buttonDNS">' +
                    'DNS' +
                '</td>' +
            '</tr>';
}

function addRowInField(currentValue, index, arr) {
    
    
    document.getElementById('inField').innerHTML += 
            '<tr class="row">' +
                '<td id="inFieldNr' + index +'" class="tbCell riderNr">' +
                    inField[index].nr +
                '</td>' +
                '<td id="inFieldName' + index +'" class="tbCell riderName">' +
                    inField[index].name +
                '</td>' +
                '<td id="inFieldTime' + index +'" class="tbCell timer">' +
                    displayTime(inField[index].time) +
                '</td>' +

                '<td id="inFieldPlusS' + index +'" class="tbCell btn smallPenalty" onclick="addSmallPenalty(' + index + ')">' +
                    '+1s' +
                '</td>' + 
                
                '<td id="inFieldS" class="tbCell penaltyCounter">' +
                    inField[index].p1 +
                '</td>' +

                '<td id="inFieldMinS' + index +'" class="tbCell btn smallPenalty" onclick="minusSmallPenalty(' + index + ')">' +
                    "-1s" +
                '</td>' +

                '<td class="empty tbCell">' +

                '</td>' +

                '<td id="inFieldPlusB' + index +'" class="tbCell btn bigPenalty" onclick="addBigPenalty(' + index + ')">' +
                    "+3s" +
                '</td>' +
                
                '<td id="inFieldB" class="tbCell penaltyCounter">' +
                    inField[index].p3 +
                '</td>' +

                '<td id="inFieldMinB' + index +'" class="tbCell btn bigPenalty" onclick="minusBigPenalty(' + index + ')">' +
                    "-3s" +
                '</td>' +

                '<td class="empty tbCell">' +

                '</td>' +

                '<td id="inFieldDNF' + index +'" class="tbCell btn dnf" onclick="flagDNF(' + index + ')">' +
                    "DNF" +
                '</td>' +

                '<td class="empty tbCell">' +

                '</td>' +

                '<td id="inFieldDSQ' + index +'" class="tbCell btn dsq" onclick="flagDSQ(' + index + ')">' +
                    "DSQ" +
                '</td>' +

                '<td class="empty tbCell">' +

                '</td>' +

                '<td id="inFieldStop' + index +'" class="tbCell btn stop" onclick="confirmStop(' + index + ')">' +
                    "Stop" +
                '</td>' + 
             '</tr>';
}



function addSmallPenalty(index) {
    console.log("Plus Small Penalty " + index);
    id = 'inFieldPlusS' + index;
    feedback(id);
}

function minusSmallPenalty(index) {
    console.log("Remove Small Penalty " + index);
    
}

function addBigPenalty(index) {
    console.log("Plus Big Penalty " + index);
    
}

function minusBigPenalty(index) {
    console.log("Remove Big penalty " + index);
    
}

function flagDNF(index) {
    console.log("DNF " + index);
    
}

function flagDSQ(index) {
    console.log("DSQ " + index);
    
}

function confirmStop(index) {
    console.log("Stop " + index);
}

function displayTime(seconds) {
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


function run() {
    while (0) {
        sleep(1000);
        console.log(".");
    }
}

function sleep(milliseconds) {
  const date = Date.now();
  let currentDate = null;
  do {
    currentDate = Date.now();
  } while (currentDate - date < milliseconds);
}


function feedback(id) {
    var element = document.getElementById(id);
    element.classList.add("feedback");
    element.classList.remove("smallPenalty");
}