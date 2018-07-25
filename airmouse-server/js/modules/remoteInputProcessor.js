const robot = require('robotjs')

module.exports = class RemoteInputProcessor {
    processInput(clientInput) {
        let curMousePos = robot.getMousePos()

        robot.moveMouse(curMousePos.x + clientInput.MouseRelativeX, curMousePos.y + clientInput.MouseRelativeY)

        if (clientInput.MouseLeftButtonPressed !== mouseLeftPressed) {
            if (clientInput.MouseLeftButtonPressed) {
                robot.mouseToggle('down', 'left')
            }
            else {
                robot.mouseToggle('up', 'left')
            }

            mouseLeftPressed = clientInput.MouseLeftButtonPressed
        }

        if (clientInput.MouseRightButtonPressed !== mouseRightPressed) {
            if (clientInput.MouseRightButtonPressed) {
                robot.mouseToggle('down', 'right')
            }
            else {
                robot.mouseToggle('up', 'right')
            }

            mouseRightPressed = clientInput.MouseRightButtonPressed
        }
    }
}

let mouseLeftPressed = false
let mouseRightPressed = false