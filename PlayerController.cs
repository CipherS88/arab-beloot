using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class PlayerController : MonoBehaviour
{
    protected int mySeatIndex;
    protected GameManager gameManager;

    // تهيئة اللاعب
    public void Setup(int index, GameManager gm)
    {
        mySeatIndex = index;
        gameManager = gm;
    }

    // 1. قرار القص
    public abstract IEnumerator DecideCut();

    // 2. قرار الشراء
    public abstract IEnumerator DecideBuy(int round, GameManager.BuyType currentProject, GameManager.CardSuit currentSuit);

    // 3. قرار المشاريع
    public abstract IEnumerator DecideProjects(List<GameManager.ProjectResult> myProjects);

    // 4. قرار لعب الكرت (تم تفعيله الآن)
    public abstract IEnumerator PlayCard(List<GameObject> myHand, List<Card> currentTrick);
}