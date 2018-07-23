const dgram = require('dgram')
const net = require('net')
const EventEmitter = require('events')
const os = require('os')

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
        this.emit('connectionChanged', this.connected)
    }

    get machineName() {
        return os.hostname()
    }

    constructor() {
        super()

        this.tcpServer = net.createServer(socket => onConnection(this, socket))
        this.udpSocket = dgram.createSocket('udp4')

        this.udpSocket.on('listening', () => this.udpSocket.setBroadcast(true))
        this.udpSocket.on('message', (msg, rinfo) => onUDPMessage(this, msg, rinfo))
    }

    start() {
        this.tcpServer.listen(serverPort, '0.0.0.0')

        this.intervalHandle = setInterval(() => {
            this.udpSocket.send(JSON.stringify({
                serverAddresses: getMachineAddresses(),
                serverPort: serverPort
            }), serverPortUDP, '0.0.0.0')
        }, 2000)

        this.running = true
    }

    stop() {
        this.tcpServer.close()
        clearInterval(this.intervalHandle)
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
    socket.write(JSON.stringify({
        OK: true
    }), 'utf8');
}

function onTCPClose(self) {
    self.tcpSocket = null
    self.isConnected = false
}

function onUDPMessage(self, message, rinfo) {
    let currentClientAddress = self.tcpSocket.address()

    if (!self.isConnected || rinfo.address !== currentClientAddress.address || rinfo.port !== currentClientAddress.port) {
        return
    }

    let msgObj = JSON.parse(message.toString())

    self.emit('dataReceived', msgObj)
}

function getMachineAddresses() {
    let addresses = []

    for (const iface in os.networkInterfaces()) {
        if (iface.internal || iface.family !== 'IPv4') return
        addresses.push(iface.address)
    }

    return addresses
}