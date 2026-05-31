using UnityEngine;

public class GridGenerator : MonoBehaviour
{
    public GameObject tilePrefab;
    public int width = 10;
    public int length = 10;
    public float spacing = 10f;

    // Baþlangýç koordinatlarýn
    public Vector3 startOffset = new Vector3(5f, 0.1f, 5f);

    void Start()
    {
        GenerateGrid();
    }

    void GenerateGrid()
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < length; z++)
            {
                // Baþlangýç noktasýný (startOffset) ekleyerek kareleri diziyoruz
                Vector3 pos = new Vector3(x * spacing, 0, z * spacing) + startOffset;

                GameObject newTile = Instantiate(tilePrefab, pos, Quaternion.identity, transform);
                newTile.name = $"Tile_{x}_{z}";
            }
        }
    }
}