using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.XR;

public class StereoscopicImageRenderSystem : MonoBehaviour {
    public Camera mainCamera;
    public Camera uiCamera;
    public Canvas canvas;
    public Shader stereoscopicImageShader;
    public RawImage playerScreen;
    public InputActionAsset inputActions;

    private Transform mainCameraTransform;
    private RenderTexture leftEyeTexture;
    private RenderTexture rightEyeTexture;
    private InputAction centerEyePosition;
    private InputAction centerEyeRotation;

    private IEnumerator Start() {
        mainCamera.enabled = false;
        mainCameraTransform = mainCamera.transform;

        var descriptor = ((RenderTexture) playerScreen.texture).descriptor;

        while (XRSettings.eyeTextureWidth == 0) {
            yield return new WaitForEndOfFrame();
        }

        descriptor.width = XRSettings.eyeTextureWidth;
        descriptor.height = XRSettings.eyeTextureHeight;
        descriptor.useMipMap = false;

        leftEyeTexture = new RenderTexture(descriptor);
        rightEyeTexture = new RenderTexture(descriptor);

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

    private void OnPreRender() {
        if (centerEyePosition == null) return;

        canvas.planeDistance = uiCamera.stereoConvergence;

        var previousTarget = mainCamera.targetTexture;
        var originalWorldToCameraMatrix = mainCamera.worldToCameraMatrix;
        var halfStereoSeparation = uiCamera.stereoSeparation / 2;

        //mainCameraTransform.localPosition = centerEyePosition.ReadValue<Vector3>();
        //mainCameraTransform.localRotation = centerEyeRotation.ReadValue<Quaternion>();

        /*var previousNearClipPlane = uiCamera.nearClipPlane;
        var previousFarClipPlane = uiCamera.farClipPlane;
        uiCamera.nearClipPlane = mainCamera.nearClipPlane;
        uiCamera.farClipPlane = mainCamera.farClipPlane;
        var leftEyeProjectionMatrix = uiCamera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left);
        var rightEyeProjectionMatrix = uiCamera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right);
        uiCamera.nearClipPlane = previousNearClipPlane;
        uiCamera.farClipPlane = previousFarClipPlane;*/

        mainCamera.fieldOfView = uiCamera.fieldOfView;

        mainCamera.worldToCameraMatrix = originalWorldToCameraMatrix * Matrix4x4.Translate(new Vector3(-halfStereoSeparation, 0, 0));
        //mainCamera.projectionMatrix = leftEyeProjectionMatrix;
        mainCamera.targetTexture = leftEyeTexture;
        mainCamera.Render();

        mainCamera.worldToCameraMatrix = originalWorldToCameraMatrix * Matrix4x4.Translate(new Vector3(halfStereoSeparation, 0, 0));
        //mainCamera.projectionMatrix = rightEyeProjectionMatrix;
        mainCamera.targetTexture = rightEyeTexture;
        mainCamera.Render();

        mainCamera.ResetWorldToCameraMatrix();
        //mainCamera.ResetProjectionMatrix();
        mainCamera.targetTexture = previousTarget;
    }

    private void OnDrawGizmos() {
        if (mainCameraTransform == null) return;

        var originalWorldToCameraMatrix = mainCamera.worldToCameraMatrix;
        var halfStereoSeparation = uiCamera.stereoSeparation / 2;
        var leftEyeMatrix = originalWorldToCameraMatrix * Matrix4x4.Translate(new Vector3(-halfStereoSeparation, 0, 0));
        var rightEyeMatrix = originalWorldToCameraMatrix * Matrix4x4.Translate(new Vector3(halfStereoSeparation, 0, 0));

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(mainCameraTransform.position, 0.01f);

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(leftEyeMatrix.MultiplyPoint(mainCameraTransform.position) + mainCameraTransform.position, 0.01f);
        Gizmos.DrawRay(leftEyeMatrix.MultiplyPoint(mainCameraTransform.position) + mainCameraTransform.position, leftEyeMatrix.MultiplyVector(new Vector3(0, 0, -1)));
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(rightEyeMatrix.MultiplyPoint(mainCameraTransform.position) + mainCameraTransform.position, rightEyeMatrix.MultiplyVector(new Vector3(0, 0, -1)));
        Gizmos.DrawSphere(rightEyeMatrix.MultiplyPoint(mainCameraTransform.position) + mainCameraTransform.position, 0.01f);
    }
}
