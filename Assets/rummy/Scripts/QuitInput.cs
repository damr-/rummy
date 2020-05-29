using UnityEngine;

public class QuitInput : MonoBehaviour
{
    private float firstPressTime = -1f;
    private readonly float detectDuration = 1f;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (firstPressTime < 0)
            {
                firstPressTime = Time.time;
            }
            else
            {
                if (Time.time - firstPressTime < detectDuration)
                    QuitApp();
                else
                    firstPressTime = -1;
            }
        }
    }

    public void QuitApp()
    {
        Application.Quit();
    }
}
