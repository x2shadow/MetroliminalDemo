using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("How often to update the FPS display (in seconds)")]
    public float updateInterval = 0.5f;
    
    [Tooltip("Font size for FPS display")]
    public int fontSize = 24;
    
    [Tooltip("Position of FPS counter")]
    public TextAnchor screenAnchor = TextAnchor.UpperLeft;
    
    [Tooltip("Color when FPS > 60")]
    public Color goodColor = Color.green;
    
    [Tooltip("Color when FPS between 30-60")]
    public Color mediumColor = Color.yellow;
    
    [Tooltip("Color when FPS < 30")]
    public Color badColor = Color.red;

    private float accum = 0; 
    private int frames = 0;
    private float timeLeft;
    private float fps;
    private GUIStyle style = new GUIStyle();
    private Rect rect;

    void Start()
    {
        timeLeft = updateInterval;
        
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = goodColor;
        style.fontSize = fontSize;
        
        // Calculate position based on anchor
        int offset = 10;
        Vector2 position = Vector2.zero;
        
        if (screenAnchor == TextAnchor.UpperLeft) position = new Vector2(offset, offset);
        else if (screenAnchor == TextAnchor.UpperRight) position = new Vector2(Screen.width - 150, offset);
        else if (screenAnchor == TextAnchor.LowerLeft) position = new Vector2(offset, Screen.height - fontSize - offset);
        else if (screenAnchor == TextAnchor.LowerRight) position = new Vector2(Screen.width - 150, Screen.height - fontSize - offset);
        
        rect = new Rect(position.x, position.y, 150, fontSize * 2);
    }

    void Update()
    {
        timeLeft -= Time.deltaTime;
        accum += Time.timeScale / Time.deltaTime;
        frames++;

        if (timeLeft <= 0.0)
        {
            fps = accum / frames;
            timeLeft = updateInterval;
            accum = 0.0f;
            frames = 0;
            
            // Update color based on FPS
            if (fps >= 60) style.normal.textColor = goodColor;
            else if (fps >= 30) style.normal.textColor = mediumColor;
            else style.normal.textColor = badColor;
        }
    }

    void OnGUI()
    {
        GUI.Label(rect, $"FPS: {fps:0.}", style);
        GUI.Label(new Rect(rect.x, rect.y + fontSize, rect.width, rect.height), $"Avg: {(1.0f / Time.smoothDeltaTime):0.}", style);
    }
}