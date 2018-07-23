const robot = require('robotjs')

module.exports = class RemoteInputProcessor {
    processInput(inputData) {
        let curMousePos = robot.getMousePos()

        robot.moveMouse(curMousePos.x + inputData.mouseRelativeX, curMousePos.y + inputData.mouseRelativeY)
    }
}