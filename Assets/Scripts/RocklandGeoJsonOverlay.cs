
using System.IO;
using CesiumForUnity;
using UnityEngine;

public class RocklandGeoJsonOverlay : MonoBehaviour
{
    private void Start()
    {
        // Locating the research GeoJSON file
        string filePath = Path.Combine(
            Application.streamingAssetsPath,
"reaches_nonlake.geojson");

        // Checking that the file exists
        if (!File.Exists(filePath))
        {
            Debug.LogError("GeoJSON file not found: " + filePath);
            return;
        }

        // Reading and parsing the GeoJSON
        string json = File.ReadAllText(filePath);
        CesiumGeoJsonDocument document =
            CesiumGeoJsonDocument.Parse(json);

        if (document == null || !document.IsValid())
        {
            Debug.LogError("Could not parse the GeoJSON file.");
            return;
        }

        // Configuring an orange river-line style
        CesiumVectorStyle style = CesiumVectorStyle.Default;
        CesiumVectorLineStyle lines = style.lineStyle;
        lines.color = new Color32(255, 140, 30, 255);
        lines.width = 4.0;
        lines.widthMode = CesiumVectorLineWidthMode.Pixels;
        style.lineStyle = lines;

        // Draping the GeoJSON lines onto Cesium terrain
        CesiumGeoJsonDocumentRasterOverlay overlay =
            gameObject.AddComponent<CesiumGeoJsonDocumentRasterOverlay>();

        // Assigning a separate material slot for the river overlay
        overlay.materialKey = "1";

        overlay.defaultStyle = style;
        overlay.document = document;

        Debug.Log("Pilot GeoJSON loaded successfully.");
    }
}
