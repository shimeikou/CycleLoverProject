using System;
using System.Collections;
using System.Linq;
using CyclingLove.Session;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class TitleToMainGameTests
{
    private const float TimeoutSeconds = 20f;

    [SetUp]
    public void SetUp() => GameSession.PlayerData = null;

    [UnityTest]
    public IEnumerator NewGameFromTitleShowsFirstDayAndIgnoresDoubleClick()
    {
        yield return SceneManager.LoadSceneAsync("InitScene");

        var deadline = Time.realtimeSinceStartup + TimeoutSeconds;
        while (SceneManager.GetActiveScene().name != "MainGameScene")
        {
            Assert.Less(Time.realtimeSinceStartup, deadline, "Title did not open MainGameScene");
            FindButton("はじめから")?.onClick.Invoke();
            yield return null;
        }

        Button next = null;
        while ((next = FindButton("次へ", "読み終える")) == null || GameSession.PlayerData == null)
        {
            Assert.Less(Time.realtimeSinceStartup, deadline, "First day narrative was not shown");
            yield return null;
        }
        var body = UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None)
            .Where(text => text.isActiveAndEnabled && text.name == "BodyText")
            .Select(text => text.text)
            .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text));
        Assert.IsNotNull(body, "Narrative body is empty");
        Assert.AreEqual(4, GameSession.PlayerData.Month);
        Assert.AreEqual(1, GameSession.PlayerData.Day);

        var revision = GameSession.PlayerData.Revision;
        next.onClick.Invoke();
        next.onClick.Invoke();
        var settle = Time.realtimeSinceStartup + 1f;
        while (Time.realtimeSinceStartup < settle) yield return null;

        Assert.AreEqual(revision + 1, GameSession.PlayerData.Revision);
    }

    private static Button FindButton(params string[] labels) =>
        UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
            .FirstOrDefault(button => button.isActiveAndEnabled && button.interactable &&
                                      labels.Contains(button.GetComponentInChildren<TMP_Text>()?.text));
}
