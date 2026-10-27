
using System.IO;
using CesiumForUnity;
using UnityEngine;

public class U1PilotOverlay : MonoBehaviour
{
    void Start()
    {
        // Read the pilot dataset from StreamingAssets.
        string path = Path.Combine(
            Application.streamingAssetsPath,
            "pilot_reaches.geojson"
        );

        if (!File.Exists(path))
        {
            Debug.LogError("Pilot GeoJSON not found: " + path);
            return;
        }

        // Parse the GeoJSON.
        CesiumGeoJsonDocument doc =
            CesiumGeoJsonDocument.Parse(File.ReadAllText(path));

        if (doc == null || !doc.IsValid())
        {
            Debug.LogError("Could not parse pilot GeoJSON.");
            return;
        }

        // Read the features and apply colors by pilot.
        CesiumGeoJsonFeature[] features =
            doc.GetRootObject().GetObjectAsFeatureCollection();

        if (features == null)
        {
            Debug.LogError("Expected a GeoJSON FeatureCollection.");
            return;
        }

        foreach (CesiumGeoJsonFeature feature in features)
        {
            string pilot = feature.GetStringProperty("pilot");
            CesiumGeoJsonObject geometry = feature.GetGeometry();

            if (geometry == null || !geometry.IsValid())
                continue;

            Color32 color;

            if (pilot == "rockland")
                color = new Color32(40, 220, 95, 255);
            else if (pilot == "evadale")
                color = new Color32(255, 190, 40, 255);
            else if (pilot == "canal")
                color = new Color32(180, 80, 255, 255);
            else
                continue;

            CesiumVectorStyle style = CesiumVectorStyle.Default;

            CesiumVectorLineStyle line = style.lineStyle;
            line.color = color;
            line.colorMode = CesiumVectorColorMode.Normal;
            line.width = 6.0;
            line.widthMode = CesiumVectorLineWidthMode.Pixels;
            style.lineStyle = line;

            // Outline the canal polygon without covering imagery.
            if (pilot == "canal")
            {
                CesiumVectorPolygonStyle polygon =
                    style.polygonStyle;

                polygon.fill = false;
                polygon.outline = true;
                polygon.outlineStyle = line;

                style.polygonStyle = polygon;
            }

            geometry.SetStyle(style);
        }

        // Place all pilots in the remaining material slot.
        CesiumGeoJsonDocumentRasterOverlay overlay =
            gameObject.AddComponent<CesiumGeoJsonDocumentRasterOverlay>();

        overlay.enabled = false;
        overlay.materialKey = "2";
        overlay.document = doc;
        overlay.enabled = true;

        Debug.Log("Pilot GeoJSON loaded: " +
                  features.Length + " features");
    }
}
