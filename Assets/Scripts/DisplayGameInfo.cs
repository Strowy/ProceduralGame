using AIR.Flume;
using Application.Interfaces;
using Infrastructure.Runtime;
using UnityEngine;
using UnityEngine.UI;

public class DisplayGameInfo : DependentBehaviour
{
    [SerializeField] private RectTransform uStartButton;
    [SerializeField] private RectTransform uSeedInput;
    [SerializeField] private GameObject uMenuCamera;

    public Text scoreText;
    public Text seedText;
    public Text floorText;

    private ISeedService _seedService;
    private IGameStateController _gameStateController;

    public void Inject(
        ISeedService seedService,
        IGameStateController gameStateController)
    {
        _seedService = seedService;
        _gameStateController = gameStateController;
    }

    // Start is called before the first frame update
    public void Start()
    {
        scoreText.text = "";
        floorText.text = "";
    }

    // Update is called once per frame
    public void Update()
    {
        DisplayScore();
        DisplayDungeonInfo();
    }

    private void DisplayScore()
    {
        var status = _gameStateController.Status;
        if (status == GameStatus.InOverworld || status == GameStatus.InDungeon)
            scoreText.text = $"Cleared: {_gameStateController.Score}";
    }

    private void DisplayDungeonInfo()
    {
        if (_gameStateController.Status == GameStatus.InDungeon)
        {
            var dc = GameObject
                .FindGameObjectWithTag("DungeonController")
                .GetComponent<DungeonController>();
            floorText.text = "Floor: " + dc.GetCurrentFloor() + " / " + dc.nfloors;
        }
        else
        {
            floorText.text = "";
        }
    }

    public void StartButtonClick()
    {
        if (!int.TryParse(seedText.text, out var result)) return;
        _seedService.SetSeed(result);
        _gameStateController.SetStatus(GameStatus.Initialise);

        // Turn off menu elements
        uStartButton.gameObject.SetActive(false);
        uSeedInput.gameObject.SetActive(false);
        uMenuCamera.SetActive(false);
    }
}