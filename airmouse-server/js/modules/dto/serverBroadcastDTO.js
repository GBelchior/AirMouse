const DTOBase = require('./dtoBase')

module.exports = class ServerBroadcastDTO extends DTOBase {

    constructor(serverName) {
        super()
        this.serverName = serverName
    }

}