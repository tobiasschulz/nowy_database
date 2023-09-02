import * as http from "http";
import * as socket_io from "socket.io";

const port = parseInt(String(process.argv[2] || 5000));

interface ServerToClientEvents {
  'v1:broadcast_message': (...data: any[]) => void;
}

interface ClientToServerEvents {
  'v1:broadcast_message': (...data: any[]) => void;
}

interface InterServerEvents {
  ping: () => void;
}

interface SocketData {
  name: string;
  age: number;
}


console.log(`Server: start on port ${port}`);

process.on('uncaughtException', function (exception) {
  // handle or ignore error
  console.log(exception);
});

const httpServer = http.createServer();
const io = new socket_io.Server<
        ClientToServerEvents,
        ServerToClientEvents,
        InterServerEvents,
        SocketData
>(httpServer, { /* options */});

io.on("connection", (socket) => {
  socket.on("v1:broadcast_message", function (...values) {
    const event_name = values[0];
    const options = values[1];
    const count_values = values[2];
    console.log(`event_name: ${JSON.stringify(event_name)}`);
    console.log(`options: ${JSON.stringify(options)}`);
    console.log(`values: ${JSON.stringify(values)}`);

    if (options['except_sender']) {
      socket.broadcast.emit(`v1:broadcast_message`, ...values);
    } else {
      io.sockets.emit(`v1:broadcast_message`, ...values);
    }
  });
});

httpServer.listen(port);

