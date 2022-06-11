using AIR.Flume;
using Application.Interfaces;
using UnityEngine;

public class Portal_World : DependentBehaviour
{
    private IGameStateController _gameStateController;
    private IPlayerService _playerService;

    public void Inject(
        IGameStateController gameStateController,
        IPlayerService playerService)
    {
        _gameStateController = gameStateController;
        _playerService = playerService;
    }

    public void Start()
    {
        gameObject.name = GetInstanceID().ToString();
    }

    public void OnTriggerEnter(Collider other)
    {
        // Player enters portal
        if (!other.gameObject.CompareTag("Player")) return;
        _playerService.MarkLocation(other.gameObject.transform.position);
        _gameStateController.TriggerEventInstance(gameObject.name, PortalType.Entrance, transform.position);
    }
}
