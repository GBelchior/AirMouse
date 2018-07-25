using System;
using System.Collections.Generic;
using System.Text;

namespace AirMouse.Shared.DTO
{
    public class ClientInputDTO
    {
        public float MouseRelativeX { get; set; }
        public float MouseRelativeY { get; set; }

        public bool MouseLeftButtonPressed { get; set; }
        public bool MouseRightButtonPressed { get; set; }
    }
}
