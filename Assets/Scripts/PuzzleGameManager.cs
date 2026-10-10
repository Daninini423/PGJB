using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class PuzzleGameManager : MonoBehaviour
{
    [Header("Puzzle Slots")]
    [SerializeField] private ShapeSlot[] puzzleSlots;

    [Header("Timer")]
    [SerializeField] private float startingTime = 30f;
    [SerializeField] private TMP_Text timerText;

    [Header("Win / Lose UI")]
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;

    private float currentTime;
    private bool gameEnded = false;

    public bool IsGameEnded
    {
        get { return gameEnded; }
    }

    private void Start()
    {
        // Set waktu awal
        currentTime = startingTime;

        // Pastikan panel tidak muncul saat game mulai
        if (winPanel != null)
        {
            winPanel.SetActive(false);
        }

        if (losePanel != null)
        {
            losePanel.SetActive(false);
        }

        UpdateTimerUI();
    }

    private void Update()
    {
        // Game sudah selesai
        if (gameEnded)
            return;

        // =====================================================
        // CEK WIN TERLEBIH DAHULU
        // =====================================================

        if (AreAllSlotsFilled())
        {
            WinGame();
            return;
        }

        // =====================================================
        // TIMER
        // =====================================================

        currentTime -= Time.deltaTime;

        if (currentTime <= 0f)
        {
            currentTime = 0f;

            UpdateTimerUI();

            LoseGame();
            return;
        }

        UpdateTimerUI();
    }

    // =========================================================
    // CEK SEMUA SLOT
    // =========================================================

    private bool AreAllSlotsFilled()
    {
        if (puzzleSlots == null || puzzleSlots.Length == 0)
        {
            return false;
        }

        foreach (ShapeSlot slot in puzzleSlots)
        {
            if (slot == null)
            {
                continue;
            }

            if (!slot.IsOccupied())
            {
                return false;
            }
        }

        return true;
    }

    // =========================================================
    // TIMER UI
    // =========================================================

    private void UpdateTimerUI()
    {
        if (timerText == null)
            return;

        int minutes = Mathf.FloorToInt(currentTime / 60f);
        int seconds = Mathf.FloorToInt(currentTime % 60f);

        timerText.text =
            string.Format(
                "{0:00}:{1:00}",
                minutes,
                seconds
            );
    }

    // =========================================================
    // WIN
    // =========================================================

    private void WinGame()
    {
        if (gameEnded)
            return;

        gameEnded = true;

        Debug.Log("YOU WIN!");

        if (winPanel != null)
        {
            winPanel.SetActive(true);
        }

        StopGame();
    }

    // =========================================================
    // LOSE
    // =========================================================

    private void LoseGame()
    {
        if (gameEnded)
            return;

        gameEnded = true;

        Debug.Log("YOU LOSE!");

        if (losePanel != null)
        {
            losePanel.SetActive(true);
        }

        StopGame();
    }

    // =========================================================
    // STOP GAME
    // =========================================================

    private void StopGame()
    {
        // Tampilkan cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Hentikan rotasi kamera
        if (PlayerCam.Instance != null)
        {
            PlayerCam.Instance.updatingRotation = false;
        }
    }

    // =========================================================
    // RESTART
    // =========================================================

    public void RestartGame()
    {
        Time.timeScale = 1f;

        Scene currentScene =
            SceneManager.GetActiveScene();

        SceneManager.LoadScene(
            currentScene.buildIndex
        );
    }
}