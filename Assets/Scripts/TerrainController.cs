using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainController : MonoBehaviour
{
    // Transform for player character to determine position
    public Transform player;
    // Terrain chunk object which is responsible for creating the terrain cells
    public Transform terrainChunk;
    // Pseudo-random generator seed value
    public int seedValue;

    // Rendering values:
    // ----- //
    // Values are intrinsically tied; the greater cellSize * chunkSize, the lower renderRange is required to be to
    // cover the seen area

    // Value determining how far away from the player character the terrain is rendered (value = number of chunks)
    public int renderRange;
    // How many units square of terrain is in a chunk
    public int chunkSize;
    // How large square each individual terrain is
    public float cellSize;
    // The height variance of the map
    public int maxHeight;
    // Biome size in number of cells wide; advanced effect, currently only affects dungeon appearance rate
    public int biomeSize;
    // Player position at a chunk scale (which chunk player is currently in)
    private int px, pz;

    private bool spawned;
    
    // Start is called before the first frame update
    void Start()
    {
        // Spawn point information
        spawned = false;
        
        // Get initial player position in terms of chunks
        px = Mathf.FloorToInt((player.position.x + 0.5f * cellSize * chunkSize) / (cellSize * chunkSize));
        pz = Mathf.FloorToInt((player.position.z + 0.5f * cellSize * chunkSize) / (cellSize * chunkSize));

        // Initial terrain production
        for (int i = -renderRange; i < renderRange+1; i++)
        {
            for (int j = -renderRange; j < renderRange+1; j++)
            {
                // Instantiate the chunk and add it to the list of extant chunks
                Object.Instantiate(terrainChunk, 
                    new Vector3((i * cellSize * chunkSize) + this.transform.position.x, 0, (j * cellSize * chunkSize) + this.transform.position.z), 
                    Quaternion.identity, 
                    this.transform);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        int dx, dz;
        
        // If the character has yet to 'spawn' - Cannot do this in Start() as terrain has yet to generate at that point
        if (!spawned)
        {
            int spawnIndex, subIndex;
            Transform spawnChild, subChild;

            // Find the zero point terrain chunk and cell
            spawnIndex = (2 * renderRange + 1) * renderRange + renderRange;
            spawnChild = this.transform.GetChild(spawnIndex);
            subIndex = (chunkSize * chunkSize / 2) - (chunkSize / 2 + 1);
            subChild = spawnChild.GetChild(subIndex);
            // Set player position
            player.GetComponent<CharacterController>().enabled = false;
            player.transform.SetPositionAndRotation(new Vector3(subChild.position.x, subChild.position.y + cellSize + 4, subChild.position.z), Quaternion.identity);
            player.GetComponent<CharacterController>().enabled = true;

            spawned = true;
        }
        
        
        dx = Mathf.FloorToInt((player.position.x + 0.5f * cellSize * chunkSize) / (cellSize * chunkSize));
        dz = Mathf.FloorToInt((player.position.z + 0.5f * cellSize * chunkSize) / (cellSize * chunkSize));

        // If player character enters a different terrain chunk, update terrain
        if (dx != px || dz != pz)
        {   
            px = dx;
            pz = dz;

            // Create new chunks as necessary
            bool notInstanced;
            for (int i = -renderRange; i < renderRange + 1; i++)
            {
                for (int j = -renderRange; j < renderRange + 1; j++)
                {
                    // Check if the tested location has been instantiated (is present in list of chunks)
                    notInstanced = true;
                    for (int k = 0; k < this.transform.childCount; k++)
                    {
                        if ((this.transform.GetChild(k).position.x == (px + i) * cellSize * chunkSize) && (this.transform.GetChild(k).position.z == (pz + j) * cellSize * chunkSize))
                        {
                            notInstanced = false;
                        }
                    }
                    // Instantiate if true (no instantiation present)
                    if (notInstanced)
                    {
                        // Instantiate the chunk and add it to the list of extant chunks
                        Object.Instantiate(terrainChunk,
                            new Vector3(((px + i) * cellSize * chunkSize) + this.transform.position.x, 0, ((pz + j) * cellSize * chunkSize) + this.transform.position.z),
                            Quaternion.identity,
                            this.transform);
                    }
                }
            }

            // Delete chunks that are now too far from player
            for (int k = 0; k < this.transform.childCount; k++)
            {
                if (Mathf.Abs(this.transform.GetChild(k).position.x - px * cellSize * chunkSize) + 
                    Mathf.Abs(this.transform.GetChild(k).position.z - pz * cellSize * chunkSize) > 
                    ((renderRange * 2 + 1) * cellSize * chunkSize))
                {
                    Object.Destroy(this.transform.GetChild(k).gameObject);
                }
            }
        }

    }
}