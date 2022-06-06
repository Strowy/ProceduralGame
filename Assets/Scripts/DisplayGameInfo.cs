using AIR.Flume;
using Application.Interfaces;
using UnityEngine;
using UnityEngine.UI;

public class DisplayGameInfo : DependentBehaviour
{
    public Text scoreText;
    public Text seedText;
    public Text floorText;

    private WorldController _wc;

    private ISeedService _seedService;

    public void Inject(ISeedService seedService)
    {
        _seedService = seedService;
    }

    // Start is called before the first frame update
    public void Start()
    {
        _wc = GameObject.FindGameObjectWithTag("WorldController").GetComponent<WorldController>();
        scoreText.text = "";
        floorText.text = "";
    }

    // Update is called once per frame
    public void Update()
    {
        if (_wc.GetState() > 0)
            scoreText.text = "Cleared: " + _wc.GetScore();

        if (_wc.GetState() == 2)
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
        // If so, grab seed value and start game
        _seedService.SetSeed(result);

        _wc.seedValue = result;
        _wc.SetState(0);

        // Turn off menu elements
        var obj = GameObject.FindGameObjectsWithTag("Menu");
        foreach (var gObj in obj)
        {
            gObj.SetActive(false);
        }
    }
}