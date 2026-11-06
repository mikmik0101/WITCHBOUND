using UnityEngine;
using UnityEngine.UI;

public class ResultsScreen : MonoBehaviour
{
    public static ResultsScreen Instance { get; private set; }
    
    [Header("Display")]
    public Text titleText;
    public Text generatorsRepairedText;
    public Text survivorsRescuedText;
    public Text survivorsEscapedText;
    public Text coinsEarnedText;
    public Text bonusText;
    
    [Header("Buttons")]
    public Button playAgainButton;
    public Button characterSelectButton;
    public Button upgradesButton;
    public Button mainMenuButton;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    private void Start()
    {
        if (playAgainButton != null)
            playAgainButton.onClick.AddListener(PlayAgain);
        if (characterSelectButton != null)
            characterSelectButton.onClick.AddListener(GoToCharacterSelect);
        if (upgradesButton != null)
            upgradesButton.onClick.AddListener(GoToUpgrades);
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(GoToMainMenu);
    }
    
    public void ShowResults(bool victory, int generatorsRepaired, int survivorsRescued, int survivorsEscaped, int coinsEarned)
    {
        if (titleText != null)
            titleText.text = victory ? "VICTORY" : "DEFEAT";
        
        if (generatorsRepairedText != null)
            generatorsRepairedText.text = $"Generators Repaired: {generatorsRepaired}/10";
        
        if (survivorsRescuedText != null)
            survivorsRescuedText.text = $"Survivors Rescued: {survivorsRescued}/3";
        
        if (survivorsEscapedText != null)
            survivorsEscapedText.text = $"Survivors Escaped: {survivorsEscaped}/4";
        
        if (coinsEarnedText != null)
            coinsEarnedText.text = $"Coins Earned: {coinsEarned}";
        
        if (bonusText != null)
            bonusText.text = victory ? "All survivors escaped! Bonus awarded." : "Better luck next time.";
        
        gameObject.SetActive(true);
    }
    
    private void PlayAgain()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RequestNewGame();
        }
        gameObject.SetActive(false);
    }
    
    private void GoToCharacterSelect()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(GameState.CharacterSelect);
        }
        gameObject.SetActive(false);
    }
    
    private void GoToUpgrades()
    {
        // Open upgrade panel
        gameObject.SetActive(false);
    }
    
    private void GoToMainMenu()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(GameState.MainMenu);
        }
        gameObject.SetActive(false);
    }
}
