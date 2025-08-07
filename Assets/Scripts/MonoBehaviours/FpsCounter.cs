using System.Text;
using FlowFieldAI;
using UnityEngine;

public class FpsCounter : MonoBehaviour
{
    private float lastTextUpdate;

    private float smoothedFps;
    private float smoothedDelta;
    private float smoothedFrameLatency;
    private float smoothedTimeLatency;
    private float smoothedProcessMillis;

    private const float SmoothingFactor = 0.5f;
    private const float TextUpdateRate = 0.35f;

    private BakeSystem bakeSystem;
    private NativeFlowField flowField;
    private CameraController cameraController;

    private GUIStyle coloredBackgroundStyle;
    private readonly StringBuilder stringBuilder = new();

    public bool RenderGUI;

    private void Start()
    {
        flowField = FindAnyObjectByType<FlowFieldConfig>().FlowField;
        cameraController = FindFirstObjectByType<CameraController>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            QualitySettings.vSyncCount = QualitySettings.vSyncCount == 1 ? 0 : 1;
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            Application.targetFrameRate = Application.targetFrameRate == -1 ? 60 : -1;
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            RenderGUI = !RenderGUI;
        }

        if (Time.time < lastTextUpdate + TextUpdateRate)
        {
            return;
        }

        lastTextUpdate = Time.time;

        var delta = Time.unscaledDeltaTime;
        var currentFps = 1 / delta;
        smoothedFps = (SmoothingFactor * currentFps) + (1f - SmoothingFactor) * smoothedFps;
        smoothedDelta = (SmoothingFactor * delta) + (1f - SmoothingFactor) * delta;
        smoothedFrameLatency = (SmoothingFactor * flowField.BakeFrameLatency) + (1f - SmoothingFactor) * flowField.BakeFrameLatency;
        smoothedTimeLatency = (SmoothingFactor * flowField.BakeTimeLatency) + (1f - SmoothingFactor) * flowField.BakeTimeLatency;
        smoothedProcessMillis = (SmoothingFactor * 1000 * flowField.BakeDispatchTime) + (1f - SmoothingFactor) * flowField.BakeDispatchTime;
    }

    private void OnGUI()
    {
        if (!RenderGUI)
        {
            return;
        }

        if (coloredBackgroundStyle == null)
        {
            var backgroundTexture = new Texture2D(1, 1);
            backgroundTexture.SetPixel(0, 0, Color.black);
            backgroundTexture.Apply();
            coloredBackgroundStyle = new GUIStyle(GUI.skin.label)
            {
                normal = { background = backgroundTexture, },
                padding = new RectOffset(10, 10, 5, 5),
                fontSize = 20
            };
        }

        stringBuilder.Clear();
        stringBuilder.Append(flowField.Width);
        stringBuilder.Append("x");
        stringBuilder.Append(flowField.Height);
        stringBuilder.Append("\n");
        stringBuilder.Append("Buffers: ");
        stringBuilder.Append(flowField.BuffersActive);
        stringBuilder.Append(" / ");
        stringBuilder.Append(flowField.BuffersAllocated);
        stringBuilder.Append(" / ");
        stringBuilder.Append(flowField.BuffersCapacity);
        stringBuilder.Append("\n");
        stringBuilder.Append("Process: ");
        stringBuilder.AppendFormat("{0:F2}", smoothedProcessMillis);
        stringBuilder.Append(" ms\n");
        stringBuilder.Append(Mathf.RoundToInt(smoothedDelta * 1000));
        stringBuilder.Append(" ms\n");
        stringBuilder.Append(Mathf.RoundToInt(smoothedFps));
        stringBuilder.Append(" fps\n");
        stringBuilder.Append("FrameLatency: ");
        stringBuilder.AppendFormat("{0:F0}", smoothedFrameLatency);
        stringBuilder.Append("\n");
        stringBuilder.Append("TimeLatency: ");
        stringBuilder.AppendFormat("{0:F1}", 1000 * smoothedTimeLatency);
        stringBuilder.Append(" ms\n");
        stringBuilder.Append("Camera: ");
        stringBuilder.Append(cameraController?.ActiveCamera.name);
        stringBuilder.Append("\n");
        stringBuilder.Append("Vsync: ");
        stringBuilder.Append(QualitySettings.vSyncCount);
        stringBuilder.Append("\n");
        stringBuilder.Append("Target FPS: ");
        stringBuilder.Append(Application.targetFrameRate);
        stringBuilder.Append("\n");
        var str = stringBuilder.ToString();

        GUI.color = Color.white;
        GUILayout.Label(str,coloredBackgroundStyle);
    }
}
