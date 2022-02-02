const jsocket = new WebSocket("ws://127.0.0.1:8080");

jsocket.onopen = function(event) {
    console.log("Connected to jSock Server!");

    console.log("Sending message to jSock Server...");
    jsocket.send("Hello, jSock Server!");
};

jsocket.onmessage = function(event) {
    console.log("Message from jSock Server: " + event.data);
};

jsocket.onerror = function(event) {
    console.log("Error connecting to the jSock Server.");
};