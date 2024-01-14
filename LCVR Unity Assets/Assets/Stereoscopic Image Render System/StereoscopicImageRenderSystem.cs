using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR;

public class StereoscopicImageRenderSystem : MonoBehaviour {
    public Camera mainCamera;
    public Camera uiCamera;
    public Shader stereoscopicImageShader;
    public RawImage playerScreen;
    public InputActionAsset inputActions;

    private Transform mainCameraTransform;
    private Transform uiCameraTransform;
    private RenderTexture leftEyeTexture;
    private RenderTexture rightEyeTexture;
    private InputAction centerEyePosition;
    private InputAction centerEyeRotation;

    private IEnumerator Start() {
        mainCamera.enabled = false;
        mainCameraTransform = mainCamera.transform;
        uiCameraTransform = uiCamera.transform;
        uiCamera.clearFlags = CameraClearFlags.Nothing;

        while (XRSettings.eyeTextureWidth == 0) {
            yield return new WaitForEndOfFrame();
        }

        leftEyeTexture = (RenderTexture) Instantiate(playerScreen.texture);
        leftEyeTexture.name = playerScreen.texture.name + " (Left Eye)";
        leftEyeTexture.width = (int)((float)leftEyeTexture.height / XRSettings.eyeTextureHeight * XRSettings.eyeTextureWidth);

        rightEyeTexture = Instantiate(leftEyeTexture);
        leftEyeTexture.name = playerScreen.texture.name + " (Right Eye)";

        var material = new Material(stereoscopicImageShader);
        playerScreen.texture = null;
        playerScreen.material = material;
        material.SetTexture("_LeftEyeTex", leftEyeTexture);
        material.SetTexture("_RightEyeTex", rightEyeTexture);

        var actionMap = inputActions.FindActionMap("VR Data");
        actionMap.Enable();
        centerEyePosition = actionMap.FindAction("centerEyePosition", true);
        centerEyeRotation = actionMap.FindAction("centerEyeRotation", true);
    }

    private void Update() {
        if (centerEyePosition == null) return;

        mainCameraTransform.localPosition = centerEyePosition.ReadValue<Vector3>();
        mainCameraTransform.localRotation = centerEyeRotation.ReadValue<Quaternion>();

        var previousTarget = mainCamera.targetTexture;

        var previousPosition = uiCameraTransform.position;
        var previousRotation = uiCameraTransform.rotation;
        var previousNearClipPlane = uiCamera.nearClipPlane;
        var previousFarClipPlane = uiCamera.farClipPlane;

        uiCameraTransform.position = mainCameraTransform.position;
        uiCameraTransform.rotation = mainCameraTransform.rotation;
        uiCamera.nearClipPlane = mainCamera.nearClipPlane;
        uiCamera.farClipPlane = mainCamera.farClipPlane;

        var leftEyeViewMatrix = uiCamera.GetStereoViewMatrix(Camera.StereoscopicEye.Left);
        var rightEyeViewMatrix = uiCamera.GetStereoViewMatrix(Camera.StereoscopicEye.Right);
        var leftEyeProjectionMatrix = uiCamera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left);
        var rightEyeProjectionMatrix = uiCamera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right);

        uiCameraTransform.position = previousPosition;
        uiCameraTransform.rotation = previousRotation;
        uiCamera.nearClipPlane = previousNearClipPlane;
        uiCamera.farClipPlane = previousFarClipPlane;

        mainCamera.fieldOfView = uiCamera.fieldOfView;

        mainCamera.worldToCameraMatrix = leftEyeViewMatrix;
        mainCamera.projectionMatrix = leftEyeProjectionMatrix;
        mainCamera.targetTexture = leftEyeTexture;
        mainCamera.Render();

        mainCamera.worldToCameraMatrix = rightEyeViewMatrix;
        mainCamera.projectionMatrix = rightEyeProjectionMatrix;
        mainCamera.targetTexture = rightEyeTexture;
        mainCamera.Render();

        mainCamera.ResetWorldToCameraMatrix();
        mainCamera.ResetProjectionMatrix();
        mainCamera.targetTexture = previousTarget;
    }
}
