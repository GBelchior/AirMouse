const $ = require('jquery')

const NetworkManager = require('../js/modules/networkManager')
const RemoteInputProcessor = require('../js/modules/remoteInputProcessor.js')

let netManager = new NetworkManager();
let inputProcessor = new RemoteInputProcessor();

$(() => {
    $('#btn-toggle-service').click(toggleService)
    netManager.on('dataReceived', d => inputProcessor.processInput(d))
    netManager.on('connectionChanged', (isConnected) => {
        if (isConnected) {
            $('body').css('background-color', 'green')
        }
        else {
            $('body').css('background-color', '')
        }
    })
})

function toggleService() {

    if (netManager.isRunning) {
        netManager.stop()
        $('#btn-toggle-service').text('Start Service')
        $('#device-id-container').hide()
    }
    else {
        netManager.start()
        $('#btn-toggle-service').text('Stop Service')
        $('#device-id').text(netManager.machineName)
        $('#device-id-container').show()
    }
}