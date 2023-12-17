using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR;

public class StereoscopicImageRenderSystem : MonoBehaviour {
    public Camera mainCamera;
    public RawImage playerScreen;
    public InputActionAsset inputActions;
    public bool depth = true;

    private RenderTexture leftEyeTexture;
    private RenderTexture rightEyeTexture;
    private InputAction leftEyePosition;
    private InputAction leftEyeRotation;
    private InputAction rightEyePosition;
    private InputAction rightEyeRotation;

    private IEnumerator Start() {
        mainCamera.enabled = false;

        var descriptor = ((RenderTexture) playerScreen.texture).descriptor;

        while (XRSettings.eyeTextureWidth == 0) {
            yield return new WaitForEndOfFrame();
        }

        descriptor.width = XRSettings.eyeTextureWidth;
        descriptor.height = XRSettings.eyeTextureHeight;

        leftEyeTexture = new RenderTexture(descriptor);
        rightEyeTexture = new RenderTexture(descriptor);

        var material = new Material(Shader.Find("LCVR/StereoscopicImage"));
        playerScreen.texture = null;
        playerScreen.material = material;
        material.SetTexture("_LeftEyeTex", leftEyeTexture);
        material.SetTexture("_RightEyeTex", rightEyeTexture);

        var actionMap = inputActions.FindActionMap("VR Data");
        actionMap.Enable();
        leftEyePosition = actionMap.FindAction("leftEyePosition", true);
        leftEyePosition.Enable();
        leftEyeRotation = actionMap.FindAction("leftEyeRotation", true);
        leftEyeRotation.Enable();
        rightEyePosition = actionMap.FindAction("rightEyePosition", true);
        rightEyePosition.Enable();
        rightEyeRotation = actionMap.FindAction("rightEyeRotation", true);
        rightEyeRotation.Enable();
    }

    private void OnPreRender() {
        if (rightEyeRotation == null) return;

        var mainCameraTransform = mainCamera.transform;
        var previousPosition = mainCameraTransform.position;
        var previousRotation = mainCameraTransform.rotation;
        var previousTarget = mainCamera.targetTexture;

        if (depth) mainCameraTransform.position = leftEyePosition.ReadValue<Vector3>();
        mainCameraTransform.rotation = leftEyeRotation.ReadValue<Quaternion>();
        mainCamera.targetTexture = leftEyeTexture;
        mainCamera.Render();

        if (depth) mainCameraTransform.position = rightEyePosition.ReadValue<Vector3>();
        mainCameraTransform.rotation = rightEyeRotation.ReadValue<Quaternion>();
        mainCamera.targetTexture = rightEyeTexture;
        mainCamera.Render();

        mainCameraTransform.position = previousPosition;
        mainCameraTransform.rotation = previousRotation;
        mainCamera.targetTexture = previousTarget;
    }
}
