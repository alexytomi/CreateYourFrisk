using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TouchControl : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    public Button fightButton;

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("Down! " + name);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("Inside! " + name);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log("Outside! " + name);
    }

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log(fightButton);
        Debug.Log(EventSystem.FindObjectsOfType<Button>());
    }

    // Update is called once per frame
    void Update()
    {
    }
}
