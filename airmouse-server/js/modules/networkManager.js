const dgram = require('dgram')
const net = require('net')
const os = require('os')
const EventEmitter = require('events')
const ServerBroadcastDTO = require('./dto/serverBroadcastDTO')

const serverPort = 23111
const serverPortUDP = 23112

module.exports = class NetworkManager extends EventEmitter {

    get isRunning() {
        return this.running
    }

    get isConnected() {
        return this.connected
    }

    set isConnected(value) {
        this.connected = value

        if (value) {
            stopBroadcasting(this)
        }

        this.emit('connectionChanged', this.connected)
    }

    get machineName() {
        return os.hostname()
    }

    get clientAddress() {
        if (!this.isConnected) return '';
        return this.tcpSocket.remoteAddress + ':' + this.tcpSocket.remotePort
    }

    constructor() {
        super()

        this.running = false

        this.tcpServer = net.createServer(socket => onConnection(this, socket))
        this.udpSocket = dgram.createSocket('udp4', (msg, rinfo) => onUDPMessage(this, msg, rinfo))
        this.udpSocket.bind(serverPortUDP, () => this.udpSocket.setBroadcast(true))
        this.serverBroadcastMessage = new ServerBroadcastDTO(this.machineName).toString()
    }

    start() {
        this.tcpServer.listen(serverPort, '0.0.0.0')
        startBroadcasting(this)
        this.running = true
    }

    stop() {
        if (this.isConnected) {
            this.tcpSocket.end()
            this.tcpSocket.destroy()
        }

        this.tcpServer.close()
        stopBroadcasting(this)
        this.running = false
    }
}

// "Métodos privados" da classe acima
function onConnection(self, clientSocket) {

    if (self.isConnected) {
        clientSocket.end()
        clientSocket.destroy()
        return
    }

    clientSocket.setEncoding('utf8')
    clientSocket.on('data', d => onTCPData(clientSocket, d))
    clientSocket.on('close', () => onTCPClose(self))

    self.tcpSocket = clientSocket
    self.isConnected = true
}

function onTCPData(socket, data) {
    socket.write('OK', 'utf8');
}

function onTCPClose(self) {
    self.tcpSocket = null
    self.isConnected = false
}

function onUDPMessage(self, message, rinfo) {
    if (!self.isConnected || rinfo.address !== self.tcpSocket.remoteAddress) {
        return
    }

    let msgObj = JSON.parse(message.toString())

    self.emit('dataReceived', msgObj)
}

function startBroadcasting(self) {
    self.intervalHandle = setInterval(() => {
        self.udpSocket.send(self.serverBroadcastMessage, serverPortUDP, '255.255.255.255')
    }, 1000)
}

function stopBroadcasting(self) {
    clearInterval(self.intervalHandle)
}