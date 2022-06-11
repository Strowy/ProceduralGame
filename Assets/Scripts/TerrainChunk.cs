using System.Collections;
using System.Collections.Generic;
using AIR.Flume;
using Application.Interfaces;
using Domain;
using UnityEngine;

public class TerrainChunk : DependentBehaviour
{
    // Map piece that is instantiated
    public Transform terrainObject;
    // Portals both active and dead
    public Transform portal_Active;
    public Transform portal_Dead;
    // Materials for the various terrain types
    public Material[] terrainMaterials;

    private int seed;
    private MapCellProcessor mapper;
    private float cellSize;
    private int chunkSize;
    private int biomeSize;
    private int maxHeight;

    private IGameStateController _gameStateController;

    public void Inject(IGameStateController gameStateController)
    {
        _gameStateController = gameStateController;
    }

    // Start is called before the first frame update
    void Start()
    {
        // Grab required values from the controller
        TerrainController controller = this.GetComponentInParent<TerrainController>();
        seed = controller.seedValue;
        cellSize = controller.cellSize;
        chunkSize = controller.chunkSize;
        biomeSize = controller.biomeSize;
        maxHeight = controller.maxHeight;
        
        // Get the map processor
        mapper = new MapCellProcessor(seed, maxHeight, (int)(biomeSize * cellSize));

        // Correct size of terrain cells
        terrainObject.localScale = new Vector3(cellSize, cellSize * 4, cellSize);
        
        // Generate the terrain cells
        // terrain chunks are square
        for (int i = 0; i < chunkSize; i++)
        {
            for (int j = 0; j < chunkSize; j++)
            {
                // Calculate x,z (z = y in 2D space) offset and position
                var xPos = (int) ((cellSize * (i - 0.5f * chunkSize) + cellSize * 0.5f) + this.transform.position.x);
                var zPos = (int) ((cellSize * (j - 0.5f * chunkSize) + cellSize * 0.5f) + this.transform.position.z);

                // Get cell data based on position
                int[] dataCell = mapper.CellData((int)(xPos / cellSize), (int)(zPos / cellSize));
                float yPos = dataCell[0] + this.transform.position.y;

                Transform mCell = Object.Instantiate(terrainObject, new Vector3(xPos, yPos, zPos), Quaternion.identity, this.transform);
                mCell.GetComponent<MeshRenderer>().material = terrainMaterials[dataCell[1]];
                
                // Special terrains
                // ----- //
                // If water terrain
                if (dataCell[2] == 1)
                {
                    BoxCollider boxCollider = mCell.GetComponent<BoxCollider>();
                    boxCollider.center = new Vector3(0, 3.5f, 0);
                    boxCollider.size = new Vector3(1, 8, 1);
                }
                // If Dungeon Portal
                if (dataCell[2] == 2)
                {
                    mCell.GetComponent<MeshRenderer>().material = terrainMaterials[8];

                    // Check if the dungeon is on the list of cleared dungeons
                    var portalType = _gameStateController.IsClearedDungeonEntrance(new IntegerPoint(xPos, zPos))
                            ? portal_Dead
                            : portal_Active;
                    Instantiate(portalType, new Vector3(xPos, yPos + cellSize * 2, zPos), Quaternion.identity, this.transform);
                }
                // If Dungeon Surrounds
                if (dataCell[2] == 3)
                {
                    mCell.GetComponent<MeshRenderer>().material = terrainMaterials[8];
                }
                if (dataCell[2] == 4)
                {
                    mCell.transform.position = new Vector3(xPos, yPos + 2 * cellSize, zPos);
                    mCell.GetComponent<MeshRenderer>().material = terrainMaterials[9];
                }

            }
        }
    }
}