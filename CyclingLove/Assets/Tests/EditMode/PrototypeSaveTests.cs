using System;
using System.Collections;
using System.IO;
using System.Threading;
using System.GameSession;
using System.Service;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class PrototypeSaveTests
{
    private string slot;
    [SetUp] public void SetUp() => slot = "test_" + Guid.NewGuid().ToString("N");
    [TearDown] public void TearDown()
    {
        string path = SaveDataService.SavePath(slot);
        if (File.Exists(path)) File.Delete(path);
        if (File.Exists(path + ".bak")) File.Delete(path + ".bak");
    }

    [UnityTest]
    public IEnumerator FileSaveResumesOnMainThreadAndRoundTrips()
    {
        return VerifyRoundTrip().ToCoroutine();
    }
    private async UniTask VerifyRoundTrip()
    {
        int thread = Thread.CurrentThread.ManagedThreadId;
        var d = new ThreeDayGameService().NewGame();
        d.NarrativePage = 1;
        var heroine = d.EnsureHeroineProgress("hero_bikeshop");
        heroine.Affection = 12;
        heroine.RouteStep = 3;
        heroine.IsMet = true;
        heroine.IsUnlocked = true;
        d.MetHeroineIds.Add("hero_bikeshop");
        await SaveDataService.Save(d, slot);
        Assert.AreEqual(thread, Thread.CurrentThread.ManagedThreadId, "Save must return to Unity's thread");
        var loaded = await SaveDataService.Load(slot);
        Assert.AreEqual(JsonUtility.ToJson(d), JsonUtility.ToJson(loaded));
        Assert.AreEqual(12, loaded.FindHeroineProgress("hero_bikeshop").Affection);
        d.Money = 1750;
        await SaveDataService.Save(d, slot);
        loaded = await SaveDataService.Load(slot);
        Assert.AreEqual(1750, loaded.Money);
        Assert.IsTrue(File.Exists(SaveDataService.SavePath(slot) + ".bak"));
    }

    [UnityTest]
    public IEnumerator CorruptAndMissingFilesFailWithoutReplacingData()
    {
        return VerifyInvalidFiles().ToCoroutine();
    }
    private async UniTask VerifyInvalidFiles()
    {
        Exception failure = null;
        try { await SaveDataService.Load(slot); } catch (Exception ex) { failure = ex; }
        Assert.IsInstanceOf<FileNotFoundException>(failure);
        string path = SaveDataService.SavePath(slot);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, "corrupt-test-payload");
        failure = null;
        try { await SaveDataService.Load(slot); } catch (Exception ex) { failure = ex; }
        Assert.IsNotNull(failure);
        Assert.AreEqual("corrupt-test-payload", File.ReadAllText(path));
    }
}
