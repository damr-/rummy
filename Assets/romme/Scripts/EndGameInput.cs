using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndGameInput : MonoBehaviour
{
    
	private float timeStart, detectDuration = 1f;
	
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
		{
			if(timeStart < 0)
			{
				timeStart = Time.time;
			}
			else
			{
				if(Time.time - timeStart < detectDuration)
					Application.Quit();
			}
		}
    }
}
