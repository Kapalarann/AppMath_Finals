using TMPro;
using UnityEngine;

public class GameUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI LivesText;
    [SerializeField] private TextMeshProUGUI TimerText;

    [SerializeField] public GameObject WinPanel;
    [SerializeField] private float Timer;
    private float startTime;
    public static GameUI Instance;

    private float currentTime;
    private bool isTimerRunning;
    private EnhancedMeshGenerator EMG;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        EMG = GetComponent<EnhancedMeshGenerator>();
        StartTimer(Timer);
    }

    private void Update()
    {
        if (!isTimerRunning) return;

        currentTime -= Time.deltaTime;

        if (currentTime <= 0)
        {
            currentTime = 0;
            isTimerRunning = false;
            OnTimerEnd();
        }

        UpdateTimerUI();
    }

    public void StartTimer(float time)
    {
        startTime = time;
        currentTime = time;
        isTimerRunning = true;
        UpdateTimerUI();
    }


    public void RestartTimer()
    {
        currentTime = startTime;
        isTimerRunning = true;
        UpdateTimerUI();
    }

    private void UpdateTimerUI()
    {
        int minutes = Mathf.FloorToInt(currentTime / 60);
        int seconds = Mathf.FloorToInt(currentTime % 60);

        TimerText.text = $"{minutes:00}:{seconds:00}";
    }

    private void OnTimerEnd()
    {
        EMG.ResetPlayer();
    }

    public void UpdateLives(int lives)
    {
        LivesText.text = "Lives: " + lives;
    }
}