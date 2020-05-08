using Harmony;
using MelonLoader;
using SimpleJSON;

namespace CameraScript
{
    public class Decoder
    {
        public static CameraCue[] GetCameraCues(string data)
        {
            var cameraCuesJSON = JSON.Parse(data);

            CameraCue[] cameraCues = new CameraCue[cameraCuesJSON["cameraCues"].Count];

            for (int i = 0; i < cameraCuesJSON["cameraCues"].Count; i++)
            {
                CameraCue cameraCue = new CameraCue
                {
                    tick = cameraCuesJSON["cameraCues"][i]["tick"],
                    tickLength = cameraCuesJSON["cameraCues"][i]["tickLength"],
                    xPos = cameraCuesJSON["cameraCues"][i]["xPos"],
                    yPos = cameraCuesJSON["cameraCues"][i]["yPos"],
                    zPos = cameraCuesJSON["cameraCues"][i]["zPos"],
                    xRot = cameraCuesJSON["cameraCues"][i]["xRot"],
                    yRot = cameraCuesJSON["cameraCues"][i]["yRot"],
                    zRot = cameraCuesJSON["cameraCues"][i]["zRot"]
                };

                cameraCues[i] = cameraCue;
            }

            return cameraCues;
        }

        public static float GetFOV(string data)
        {
            var cameraCuesJSON = JSON.Parse(data);

            return cameraCuesJSON["fov"];
        }
    }
}
