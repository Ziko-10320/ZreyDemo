using UnityEngine;
using UnityEngine.UI;

public class TutorialBookUI : MonoBehaviour
{
    [Header("Pages")]
    [SerializeField] private GameObject[] pages;

    [Header("Buttons")]
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;
    [SerializeField] private Button okButton;

    private int currentPage = 0;

    void Start()
    {
        Time.timeScale = 0f;
        leftButton.onClick.AddListener(GoToPreviousPage);
        rightButton.onClick.AddListener(GoToNextPage);
        okButton.onClick.AddListener(CloseBook);

        LoadPage(0);
    }

    private void LoadPage(int index)
    {
        // Disable all pages then enable only the current one
        foreach (GameObject page in pages)
            page.SetActive(false);

        pages[index].SetActive(true);
        currentPage = index;

        leftButton.gameObject.SetActive(currentPage > 0);
        rightButton.gameObject.SetActive(currentPage < pages.Length - 1);
        okButton.gameObject.SetActive(currentPage == pages.Length - 1);
    }

    private void GoToNextPage() => LoadPage(currentPage + 1);
    private void GoToPreviousPage() => LoadPage(currentPage - 1);

    private void CloseBook()
    {
        gameObject.SetActive(false);
        Time.timeScale = 1f;
    }
}