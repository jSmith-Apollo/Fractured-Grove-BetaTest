using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class settings : MonoBehaviour
{

    public Slider scroll;
    public Canvas settingsCanva;
    public Canvas UICanvas;
    public PlayerCam CameraScript;
    private float movespeedBefore;
    public PlayerMovement playerMovementClass;
    private float SensitivitySave = 1800f;
    private bool Debounce = true;

    // Start is called before the first frame update
    void Start()
    {
        scroll.value = CameraScript.sensX;
        scroll.onValueChanged.AddListener((v) =>
        {
            SensitivitySave = scroll.value;
            GameObject.Find("Value").GetComponent<Text>().text = "Value = " + scroll.value;
        });
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.Escape) && Debounce)
        {
            Debounce = false;
            if (settingsCanva.enabled)
            {
                settingsCanva.enabled = false;
                UICanvas.enabled = true;
                playerMovementClass.SetMoveSpeed(movespeedBefore);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                CameraScript.sensX = SensitivitySave;
                CameraScript.sensY = SensitivitySave;
            }
            else
            {
                UICanvas.enabled = false;
                settingsCanva.enabled = true;
                movespeedBefore = playerMovementClass.GetMoveSpeed();
                playerMovementClass.SetMoveSpeed(0);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                CameraScript.sensX = 0;
                CameraScript.sensY = 0;

            }
            Invoke(nameof(endDebounce), 0.5f);
        }
    }

    void endDebounce()
    {
        Debounce = true;
    }
}