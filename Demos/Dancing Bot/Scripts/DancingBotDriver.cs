using UnityEngine;

namespace FeedFM.Demos.Dancing_Bot.Scripts
{
    internal class DancingBotDriver : MonoBehaviour
    {
        private void Awake()
        {
            // Lock framerate to prevent wear an tear on GPU. By default, the value is set to -1
            // This means go as fast as the hardware/OS can go.
            // For PC is that can be a big issue.
            Application.targetFrameRate = 60;
        }
    }
}