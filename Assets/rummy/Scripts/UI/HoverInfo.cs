using UnityEngine;

public class HoverInfo : MonoBehaviour
{
    public GameObject hoverUI;

    public void OnMouseEnter()
    {
        hoverUI.SetActive(true);
    }
    public void OnMouseExit()
    {
        hoverUI.SetActive(false);
    }
}
