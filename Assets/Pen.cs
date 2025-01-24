using System.Collections.Generic;
using UnityEngine;

public class Pen : MonoBehaviour
{
    public Transform tip;
    public Material drawMaterial;
    public Material tipMaterial;

    [Range(0.001f, 0.1f)] public float penWidthMin = 0.001f;
    [Range(0.001f, 0.1f)] public float penWidthMax = 0.02f;

    public Color[] penColors;

    private LineRenderer currentDrawing;
    private List<Vector3> position = new();
    private List<GameObject> lineHistory = new(); // Store all drawn lines for Undo
    private int index;

    private float currentLineWidth;
    private AnimationCurve brushWidthCurve;
    public float eraseRadius = 0.05f; // Radius for eraser functionality
    private bool isEraserActive = false; // Eraser mode toggle
    private Color lastColor;
    private Texture lastTexture;
    private bool wasTextureApplied = false;
    private void Start()
    {
        currentLineWidth = penWidthMin;
        brushWidthCurve = CreateBrushWidthCurve(); 
        lastColor = Color.white;
        lastTexture = null;
        wasTextureApplied = false;
    }

    private void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.One))
        {
            isEraserActive = !isEraserActive;
            UpdateTipMaterial();
        }

        if (OVRInput.GetDown(OVRInput.Button.Two))
        {
            UndoLastLine(); 
        }
        if (isEraserActive)
        {
            Erase();
        }
        else
        {
            Draw();
        }

        if (Input.GetKeyDown(KeyCode.U))
        {
            UndoLastLine();
        }
    }

    private void Draw()
    {
        bool drawInput = Input.GetMouseButton(0);
        float triggerValue = 0;

        if (OVRPlugin.initialized)
        {
            triggerValue = Mathf.Max(OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger), OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger));
            drawInput = triggerValue > 0.001f;
        }

        if (!drawInput)
        {
            currentDrawing = null;
            return;
        }

        currentLineWidth = wasTextureApplied
            ? Mathf.Lerp(penWidthMax, 2 * penWidthMax, triggerValue)
            : brushWidthCurve.Evaluate(triggerValue);

        if (currentDrawing == null)
        {
            index = 0;
            GameObject newLine = new GameObject("Line");
            currentDrawing = newLine.AddComponent<LineRenderer>();
            lineHistory.Add(newLine);

            currentDrawing.material = new Material(drawMaterial);
            currentDrawing.startWidth = currentDrawing.endWidth = currentLineWidth;
            currentDrawing.positionCount = 1;
            currentDrawing.numCornerVertices = 200;
            currentDrawing.numCapVertices = 200;
            currentDrawing.textureMode = LineTextureMode.Tile;
            currentDrawing.SetPosition(0, tip.transform.position);
            currentDrawing.widthCurve = new AnimationCurve();
        }
        else
        {
            Vector3 currentPosition = currentDrawing.GetPosition(index);
            if (Vector3.Distance(currentPosition, tip.transform.position) > 0.01f)
            {
                index++;
                currentDrawing.positionCount = index + 1;
                currentDrawing.SetPosition(index, tip.transform.position);
                float lineLength = CalculateLineLength(currentDrawing);
                currentDrawing.material.mainTextureScale = new Vector2(lineLength, 1);
                UpdateWidthCurve();
            }
        }
    }


    private void Erase()
    {
        LineRenderer[] allLines = FindObjectsOfType<LineRenderer>();
        Vector3 eraserPosition = tip.transform.position;

        foreach (LineRenderer line in allLines)
        {
            for (int i = 0; i < line.positionCount; i++)
            {
                Vector3 point = line.GetPosition(i);
                if (Vector3.Distance(eraserPosition, point) <= eraseRadius)
                {
                    List<Vector3> newPositions = new List<Vector3>();
                    for (int j = 0; j < line.positionCount; j++)
                    {
                        if (j != i) newPositions.Add(line.GetPosition(j));
                    }

                    line.positionCount = newPositions.Count;
                    line.SetPositions(newPositions.ToArray());
                    break;
                }
            }
        }
    }

    public  void UndoLastLine()
    {
        if (lineHistory.Count > 0)
        {
            GameObject lastLine = lineHistory[lineHistory.Count - 1];
            lineHistory.RemoveAt(lineHistory.Count - 1);
            Destroy(lastLine);
        }
    }

    private void UpdateTipMaterial()
    {
        if (isEraserActive)
        {
            lastColor = tipMaterial.color;
            lastTexture = tipMaterial.GetTexture("_MainTex");

            tipMaterial.color = Color.white; 
            tipMaterial.SetColor("_EmissionColor", Color.white * 1f);
            tipMaterial.SetColor("_Maintex", Color.white * 1f);
            tipMaterial.SetTexture("_MainTex", null);
            tipMaterial.SetTexture("_EmissionMap", null);
        }
        else
        {
            tipMaterial.color = lastColor;
            if (lastTexture != null && !wasTextureApplied)
            {
                tipMaterial.SetTexture("_MainTex", lastTexture);
                tipMaterial.SetTexture("_EmissionMap", lastTexture);
            }

            tipMaterial.SetColor("_EmissionColor", tipMaterial.color * 1f); 
        }
    }

    public void ApplyTexture(Texture2D texture, Vector2 tiling)
    {
        lastColor = tipMaterial.color;
        lastTexture = texture;
        wasTextureApplied = true;

        tipMaterial.SetTexture("_MainTex", texture);
        tipMaterial.SetTexture("_EmissionMap", texture);
        tipMaterial.color = Color.white;
        tipMaterial.SetColor("_EmissionColor", Color.white);
        tipMaterial.mainTextureScale = tiling;

        currentLineWidth = Mathf.Clamp(currentLineWidth * 2f, penWidthMax, 2 * penWidthMax);
    }


    public void SwitchColor(Color color)
    {
        lastTexture = null;
        wasTextureApplied = false;

        lastColor = tipMaterial.color;
        tipMaterial.SetTexture("_MainTex", null);
        tipMaterial.SetTexture("_EmissionMap", null);
        tipMaterial.color = color;
        tipMaterial.SetColor("_EmissionColor", color * 1f); 
    }

    private float CalculateLineLength(LineRenderer lineRenderer)
    {
        float length = 0f;

        for (int i = 1; i < lineRenderer.positionCount; i++)
        {
            length += Vector3.Distance(lineRenderer.GetPosition(i - 1), lineRenderer.GetPosition(i));
        }

        return length;
    }

    private void UpdateWidthCurve()
    {
        AnimationCurve widthCurve = currentDrawing.widthCurve;
        Keyframe[] keys = new Keyframe[index + 1];

        for (int i = 0; i <= index; i++)
        {
            float t = i / (float)index;
            float width = i == index ? currentLineWidth : widthCurve.Evaluate(t);
            keys[i] = new Keyframe(t, width);
        }

        widthCurve.keys = keys;
        currentDrawing.widthCurve = widthCurve;

        currentDrawing.startWidth = currentDrawing.widthCurve.Evaluate(0f);
        currentDrawing.endWidth = currentDrawing.widthCurve.Evaluate(1f);
    }



    private AnimationCurve CreateBrushWidthCurve()
    {
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, penWidthMin);
        curve.AddKey(1f, penWidthMax);
        return curve;
    }



    public void ResetPen()
    {
        tipMaterial.SetTexture("_MainTex", null);
        tipMaterial.SetTexture("_EmissionMap", null);
        tipMaterial.color = Color.white;
        tipMaterial.SetColor("_EmissionColor", Color.white);
        currentLineWidth = penWidthMin; // Reset line width
    }
}
