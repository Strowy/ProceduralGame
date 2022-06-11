using AIR.Flume;
using Application.Interfaces;
using UnityEngine;

public class Portal_Dungeon : DependentBehaviour
{
    private IDungeonController _dungeonController;

    public void Inject(IDungeonController dungeonController)
    {
        _dungeonController = dungeonController;
    }

    public void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("Player")) return;
        _dungeonController.SetGoalFlag(true);
        _dungeonController.UpdateSeedLocation(transform.position);
    }
}
