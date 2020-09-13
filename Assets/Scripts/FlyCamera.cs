using System;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FlyCamera : MonoBehaviour
{
    public float cameraSensitivity = 10f;
    public float rollSpeed = 40f;
    public float normalMoveSpeed = 2f;
    public float slowMoveFactor = 0.25f;
    public float fastMoveFactor = 3f;
    public float speedMultiplySpeed = 2f;
    public float speedMultiply = 1f;

    private bool cursorLocked = false;
    private Quaternion rotation = Quaternion.identity;

    private void Start()
    {
        if (cursorLocked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void Update()
    {
        if (cursorLocked)
        {
            transform.localRotation *= Quaternion.AngleAxis(Input.GetAxis("Mouse X") * cameraSensitivity, Vector3.up);
            transform.localRotation *= Quaternion.AngleAxis(Input.GetAxis("Mouse Y") * cameraSensitivity, Vector3.left);

            Vector3 direction;

            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                direction = transform.forward * (normalMoveSpeed * fastMoveFactor) * Input.GetAxis("Vertical") * Time.deltaTime * speedMultiply;
                direction += transform.right * (normalMoveSpeed * fastMoveFactor) * Input.GetAxis("Horizontal") * Time.deltaTime * speedMultiply;
            }
            else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                direction = transform.forward * (normalMoveSpeed * slowMoveFactor) * Input.GetAxis("Vertical") * Time.deltaTime * speedMultiply;
                direction += transform.right * (normalMoveSpeed * slowMoveFactor) * Input.GetAxis("Horizontal") * Time.deltaTime * speedMultiply;
            }
            else
            {
                direction = transform.forward * normalMoveSpeed * Input.GetAxis("Vertical") * Time.deltaTime * speedMultiply;
                direction += transform.right * normalMoveSpeed * Input.GetAxis("Horizontal") * Time.deltaTime * speedMultiply;
            }

            transform.position += direction;

            speedMultiply *= 1 + Input.GetAxis("Mouse ScrollWheel") * speedMultiplySpeed;
            speedMultiply = Mathf.Max(0, speedMultiply);


            if (Input.GetKey(KeyCode.Q))
            {
                transform.localRotation *= Quaternion.AngleAxis(rollSpeed * Time.deltaTime, Vector3.forward);
            }
            if (Input.GetKey(KeyCode.E))
            {
                transform.localRotation *= Quaternion.AngleAxis(rollSpeed * Time.deltaTime, Vector3.back);
            }
        }

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape))
        {
            cursorLocked = !cursorLocked;
            Cursor.lockState = cursorLocked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !cursorLocked;
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            ScreenCapture.CaptureScreenshot(Application.dataPath.Replace("Assets", "Screenshots") + string.Format("{0:yyyy-MM-dd_hh-mm-ss-tt}", DateTime.Now) + ".png", 2);
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            Application.Quit();
        }
    }
}
