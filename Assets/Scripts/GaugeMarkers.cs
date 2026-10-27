using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CesiumForUnity;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

// Reading gauges_unity.json without changing the original research dataset
[Serializable]
public class GaugeObservation
{
    public string date;
    public float stage_m;
    public bool stage_available;
    public float discharge_cfs;
    public bool discharge_available;
}

[Serializable]
public class GaugeRecord
{
    public string site_no;
    public string name;
    public string note;
    public double lat;
    public double lon;
    public string stage_units;
    public string stage_start;
    public string stage_end;
    public float q90_cfs;
    public float drought_2011_min_stage_m;
    public GaugeObservation[] monthly;
    public GaugeObservation[] daily_2011;
}

[Serializable]
public class GaugeCollection
{
    public GaugeRecord[] gauges;
}

// Replaces the earlier GaugeMarkers.cs. Attach only once, to CesiumGeoreference.
public class GaugeMarkers : MonoBehaviour
{
    [Header("Prototype marker appearance")]
    [Tooltip("Diameter in Unity meters; intentionally oversized for map testing")]
    public float markerDiameterMeters = 90f;

    [Tooltip("Height above the WGS84 ellipsoid, not actual gauge datum/elevation")]
    public double markerHeightAboveEllipsoid = 250.0;

    private GaugeCollection gaugeData;
    private GaugeRecord selectedGauge;
    private bool show2011Daily = false;
    private readonly Dictionary<GameObject, GaugeRecord> markerLookup =
        new Dictionary<GameObject, GaugeRecord>();

    private static readonly Color StageColor = new Color(0.16f, 0.87f, 1f);
    private static readonly Color DischargeColor = new Color(1f, 0.8f, 0.24f);

    private void Start()
    {
        // Loading a Unity-ready version of the source file
        string path = Path.Combine(Application.streamingAssetsPath, "gauges_unity.json");
        if (!File.Exists(path))
        {
            Debug.LogError("Missing gauge file: " + path);
            return;
        }
        gaugeData = JsonUtility.FromJson<GaugeCollection>(File.ReadAllText(path));
        if (gaugeData == null || gaugeData.gauges == null || gaugeData.gauges.Length == 0)
        {
            Debug.LogError("Could not read gauges_unity.json");
            return;
        }

        Color[] colors = {
            new Color(1f, 0.72f, 0.1f),
            new Color(0.20f, 0.76f, 1f),
            new Color(1f, 0.32f, 0.52f),
            new Color(0.36f, 0.90f, 0.34f)
        };

        for (int i = 0; i < gaugeData.gauges.Length; i++)
        {
            GaugeRecord gauge = gaugeData.gauges[i];
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = "USGS_" + gauge.site_no;
            marker.transform.SetParent(transform, false);
            marker.transform.localScale = Vector3.one * markerDiameterMeters;

            Renderer renderer = marker.GetComponent<Renderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader != null) renderer.material = new Material(shader);
            renderer.material.color = colors[i % colors.Length];

            CesiumGlobeAnchor anchor = marker.AddComponent<CesiumGlobeAnchor>();
            anchor.longitudeLatitudeHeight = new double3(
                gauge.lon, gauge.lat, markerHeightAboveEllipsoid);
            markerLookup[marker] = gauge;
        }

        // Selecting Rockland first because the test camera is currently over Rockland
        selectedGauge = Array.Find(gaugeData.gauges, g => g.site_no == "08033500")
                        ?? gaugeData.gauges[0];
        Debug.Log("GaugeMarkers: created " + markerLookup.Count + " markers; charts ready.");
    }

    private void Update()
    {
        // Allowing sphere selection in addition to station buttons
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame ||
            Camera.main == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, 500000f) &&
            markerLookup.TryGetValue(hit.collider.gameObject, out GaugeRecord gauge))
        {
            selectedGauge = gauge;
            Debug.Log("Selected USGS gauge: " + gauge.name);
        }
    }

    private void OnGUI()
    {
        if (gaugeData == null || gaugeData.gauges == null) return;

        // Listing all four gauges even when the map is centered on Rockland
        GUI.Box(new Rect(12, 12, 320, 172), "USGS Gauges - select a station");
        for (int i = 0; i < gaugeData.gauges.Length; i++)
        {
            GaugeRecord g = gaugeData.gauges[i];
            if (GUI.Button(new Rect(22, 37 + i * 33, 300, 29),
                g.name + " (" + g.site_no + ")"))
                selectedGauge = g;
        }

        if (selectedGauge == null) return;

        GUI.Box(new Rect(12, 193, 340, 127), "Selected station");
        GUI.Label(new Rect(22, 215, 320, 20), selectedGauge.name);
        GUI.Label(new Rect(22, 237, 320, 20), "USGS " + selectedGauge.site_no);
        GUI.Label(new Rect(22, 258, 320, 20),
            "Lat " + selectedGauge.lat.ToString("F5") +
            "  Lon " + selectedGauge.lon.ToString("F5"));
        GUI.Label(new Rect(22, 279, 320, 32),
            "2011 minimum stage: " +
            selectedGauge.drought_2011_min_stage_m.ToString("F3") +
            " m (local gauge datum)");

        // Using the right side of the game window for historical graphs
        float panelWidth = Mathf.Min(470f, Screen.width * 0.49f);
        float px = Screen.width - panelWidth - 12f;
        float py = 12f;
        float ph = Mathf.Min(535f, Screen.height - 24f);
        if (panelWidth < 285f || ph < 365f) return;

        GUI.Box(new Rect(px, py, panelWidth, ph), "Gauge history: " + selectedGauge.site_no);
        GUI.Label(new Rect(px + 12, py + 25, panelWidth - 20, 22), selectedGauge.name);

        float buttonW = (panelWidth - 32f) * .5f;
        if (GUI.Button(new Rect(px + 10, py + 51, buttonW, 27),
            show2011Daily ? "Full history (monthly)" : "Full history  [selected]"))
            show2011Daily = false;
        if (GUI.Button(new Rect(px + 22 + buttonW, py + 51, buttonW, 27),
            show2011Daily ? "2011 daily  [selected]" : "2011 daily detail"))
            show2011Daily = true;

        GaugeObservation[] rows = show2011Daily
            ? selectedGauge.daily_2011 : selectedGauge.monthly;
        float chartW = panelWidth - 78f;
        float chartH = Mathf.Max(75f, (ph - 200f) / 2f);
        Rect first = new Rect(px + 58f, py + 126f, chartW, chartH);
        Rect second = new Rect(px + 58f, py + 126f + chartH + 54f, chartW, chartH);

        DrawChart(first, rows, true, show2011Daily);
        DrawChart(second, rows, false, show2011Daily);

        GUI.Label(new Rect(px + 12, py + 83f, panelWidth - 24, 20),
            show2011Daily
                ? "2011 daily observations; gaps are not interpolated."
                : "Monthly means; orange vertical band marks 2011.");
    }

    private static void DrawChart(Rect rect, GaugeObservation[] rows, bool stage, bool daily)
    {
        string title = stage ? "Water stage (m; relative to gauge datum)"
                             : "Discharge (cfs)";
        GUI.Label(new Rect(rect.x - 35, rect.y - 23, rect.width + 36, 20), title);
        FillRect(rect, new Color(0.06f, 0.09f, 0.12f, 0.88f));
        if (rows == null || rows.Length == 0)
        {
            GUI.Label(rect, "No observations available");
            return;
        }

        // Time coordinate is real calendar time, not array index
        int startT = int.MaxValue;
        int endT = int.MinValue;
        float minY = float.MaxValue;
        float maxY = float.MinValue;
        for (int i = 0; i < rows.Length; i++)
        {
            int t = TimeIndex(rows[i].date, daily);
            if (t < 0) continue;
            if (t < startT) startT = t;
            if (t > endT) endT = t;
            bool available = stage ? rows[i].stage_available : rows[i].discharge_available;
            if (!available) continue;
            float v = stage ? rows[i].stage_m : rows[i].discharge_cfs;
            if (float.IsNaN(v) || float.IsInfinity(v)) continue;
            minY = Mathf.Min(minY, v);
            maxY = Mathf.Max(maxY, v);
        }
        if (startT >= endT || minY == float.MaxValue)
        {
            GUI.Label(rect, "No values available");
            return;
        }

        // Highlighting the entire year 2011 on full-history charts
        if (!daily)
        {
            int a = 2011 * 12;
            int b = 2012 * 12;
            float xA = rect.x + rect.width * Mathf.Clamp01((float)(a - startT) / (endT - startT));
            float xB = rect.x + rect.width * Mathf.Clamp01((float)(b - startT) / (endT - startT));
            if (xB > xA) FillRect(new Rect(xA, rect.y, xB - xA, rect.height),
                new Color(1f, 0.47f, 0.05f, 0.40f));
        }

        float range = maxY - minY;
        if (range < 0.00001f) range = 1f;
        float bottom = minY - 0.04f * range;
        float top = maxY + 0.04f * range;
        float denom = top - bottom;

        DrawLine(new Vector2(rect.x, rect.y + rect.height),
                 new Vector2(rect.x + rect.width, rect.y + rect.height),
                 new Color(1, 1, 1, 0.65f), 1f);
        GUI.Label(new Rect(rect.x - 56, rect.y - 2, 53, 20),
            FormatValue(maxY, stage));
        GUI.Label(new Rect(rect.x - 56, rect.y + rect.height - 19, 53, 20),
            FormatValue(minY, stage));

        Vector2 prev = Vector2.zero;
        int prevTime = int.MinValue;
        bool hadPrev = false;
        Color lineColor = stage ? StageColor : DischargeColor;
        for (int i = 0; i < rows.Length; i++)
        {
            int t = TimeIndex(rows[i].date, daily);
            bool available = stage ? rows[i].stage_available : rows[i].discharge_available;
            if (t < 0 || !available)
            {
                hadPrev = false;
                continue;
            }
            float v = stage ? rows[i].stage_m : rows[i].discharge_cfs;
            if (float.IsNaN(v) || float.IsInfinity(v)) { hadPrev = false; continue; }
            float x = rect.x + rect.width * (t - startT) / (endT - startT);
            float y = rect.y + rect.height * (1f - (v - bottom) / denom);
            Vector2 point = new Vector2(x, y);
            // Avoid visually inventing observations across missing-data gaps
            if (hadPrev && t - prevTime <= 2)
                DrawLine(prev, point, lineColor, 2f);
            prev = point;
            prevTime = t;
            hadPrev = true;
        }

        GUI.Label(new Rect(rect.x, rect.y + rect.height + 2, rect.width * .5f, 18),
            daily ? "Jan 2011" : (startT / 12).ToString());
        GUI.Label(new Rect(rect.x + rect.width * .52f, rect.y + rect.height + 2,
                           rect.width * .48f, 18),
            daily ? "Dec 2011" : (endT / 12).ToString());
    }

    private static int TimeIndex(string date, bool daily)
    {
        if (string.IsNullOrEmpty(date)) return -1;
        if (daily)
        {
            if (DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out DateTime day)) return day.DayOfYear - 1;
            return -1;
        }
        if (date.Length < 7) return -1;
        if (!int.TryParse(date.Substring(0, 4), out int year) ||
            !int.TryParse(date.Substring(5, 2), out int month) || month < 1 || month > 12)
            return -1;
        return year * 12 + month - 1;
    }

    private static string FormatValue(float value, bool stage)
    {
        return stage ? value.ToString("F2", CultureInfo.InvariantCulture)
                     : value.ToString("F0", CultureInfo.InvariantCulture);
    }

    private static void FillRect(Rect rectangle, Color color)
    {
        Color oldColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rectangle, Texture2D.whiteTexture);
        GUI.color = oldColor;
    }

    private static void DrawLine(Vector2 from, Vector2 to, Color color, float thickness)
    {
        Vector2 delta = to - from;
        if (delta.sqrMagnitude < 0.000001f) return;
        Matrix4x4 savedMatrix = GUI.matrix;
        Color savedColor = GUI.color;
        GUI.color = color;
        GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, from);
        GUI.DrawTexture(new Rect(from.x, from.y - thickness * 0.5f,
                                 delta.magnitude, thickness), Texture2D.whiteTexture);
        GUI.matrix = savedMatrix;
        GUI.color = savedColor;
    }
}
