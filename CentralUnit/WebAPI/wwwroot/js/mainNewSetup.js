
var serverRequestInterval = 1000;





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



var serverDataTxt = "";
var riderInStartBoxId = "";

async function getDataFromServer() {
    console.log("Hello server.");
    
    var response = await fetch("/racetracking/state");
    var data = await response.json();
    
    oldServerDataTxt = serverDataTxt;
    serverDataTxt = JSON.stringify(data);
    
    // only continue if server data changed:
    if (oldServerDataTxt === serverDataTxt) {
        return;
    }
    
    riderInStartBoxId = data["waiting"];
    console.log(riderInStartBoxId);
}


setInterval(getDataFromServer, serverRequestInterval);